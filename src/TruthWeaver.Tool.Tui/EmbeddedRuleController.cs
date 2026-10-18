namespace TruthWeaver.Tool.Tui;

using System.Runtime.CompilerServices;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core;

/// <summary>
/// The logic of editing a rule that lives inside a larger JSON or YAML document: pick the rule by a path or by browsing the
/// document, switch between the rules of the document, and save the rule back into the document. The keys call these
/// methods. The workbench document holds the rule alone; this controller keeps the file and the path, and takes over
/// <see cref="Workbench.Save"/> while the rule is open. The document file is never written without a confirmation.
/// </summary>
internal sealed class EmbeddedRuleController
{
    private static readonly ConditionalWeakTable<Workbench, EmbeddedRuleController> Controllers = [];

    private readonly Workbench workbench;
    private readonly WorkbenchServices? services;
    private EmbeddedRuleSession? openSession;

    /// <summary>Initializes a new instance of the <see cref="EmbeddedRuleController"/> class.</summary>
    /// <param name="workbench">The shared state.</param>
    /// <param name="services">The dialogs, or <see langword="null"/> for <see cref="WorkbenchServices.Current"/>.</param>
    public EmbeddedRuleController(Workbench workbench, WorkbenchServices? services = null)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        this.workbench = workbench;
        this.services = services;
    }

    /// <summary>Gets the open rule's session, or <see langword="null"/> when the workbench document is not a rule of a larger document.</summary>
    public EmbeddedRuleSession? Session
    {
        // Another document in the workbench clears the save override, and the session ends with it.
        get => this.workbench.SaveOverride is null ? null : this.openSession;
        private set => this.openSession = value;
    }

    private IWorkbenchDialogs Dialogs => (this.services ?? WorkbenchServices.Current).Dialogs;

    /// <summary>Gets the controller of a workbench. The keys use the same controller each time.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <returns>The controller.</returns>
    public static EmbeddedRuleController For(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        return Controllers.GetValue(workbench, key => new EmbeddedRuleController(key));
    }

    /// <summary>
    /// Asks for a document file, then for the rule: a typed path or a pick from the document tree. Unsaved changes need the
    /// author's consent first.
    /// </summary>
    /// <returns><see langword="true"/> when a rule was opened.</returns>
    public bool Open()
    {
        if (this.workbench.Document.IsDirty && !this.workbench.Prompts.ConfirmDiscard("open another file"))
        {
            return false;
        }

        return this.workbench.Prompts.AskOpenPath() is { } file && this.OpenFile(file);
    }

    /// <summary>Asks how to pick the rule of a document file, and opens it.</summary>
    /// <param name="file">The document file.</param>
    /// <returns><see langword="true"/> when a rule was opened.</returns>
    public bool OpenFile(string file)
    {
        ArgumentNullException.ThrowIfNull(file);

        ToolResult<IReadOnlyList<DocumentNode>> outline = EmbeddedRuleSession.Outline(file);
        if (!outline.TryGetValue(out IReadOnlyList<DocumentNode>? nodes, out EquatableArray<Diagnostic> diagnostics))
        {
            this.workbench.SetStatus(Describe(diagnostics));
            return false;
        }

        int? how = this.Dialogs.Choose(
            $"Pick the rule in {Path.GetFileName(file)}",
            ["Browse the document tree", "Type a path (use * to match several)"]
        );
        if (how is null)
        {
            return false;
        }

        if (how == 0)
        {
            return this.PickFrom(file, nodes, "Browse the document (Enter picks)");
        }

        return this.Dialogs.AskText("Path of the rule (for example $.rules.publish or rules.*)", "$") is { } pattern
            && this.OpenAt(file, pattern);
    }

    /// <summary>
    /// Opens the rule at a path pattern. A path that names nothing, or a value that is not a rule, reports why and changes
    /// nothing. A pattern with a <c>*</c> that names several values asks which one to open.
    /// </summary>
    /// <param name="file">The document file.</param>
    /// <param name="pattern">The path, or a path with <c>*</c> segments.</param>
    /// <returns><see langword="true"/> when a rule was opened.</returns>
    public bool OpenAt(string file, string pattern)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(pattern);

        if (!pattern.Contains('*', StringComparison.Ordinal))
        {
            return this.Show(file, pattern);
        }

        ToolResult<IReadOnlyList<DocumentNode>> outline = EmbeddedRuleSession.Outline(file);
        if (!outline.TryGetValue(out IReadOnlyList<DocumentNode>? nodes, out EquatableArray<Diagnostic> diagnostics))
        {
            this.workbench.SetStatus(Describe(diagnostics));
            return false;
        }

        if (!DocumentOutline.Match(nodes, pattern).TryGetValue(out IReadOnlyList<DocumentNode>? matches, out diagnostics))
        {
            this.workbench.SetStatus(Describe(diagnostics));
            return false;
        }

        matches = [.. matches.Where(m => m.Kind != DocumentNodeKind.Container)];
        switch (matches.Count)
        {
            case 0:
                this.workbench.SetStatus($"The path '{pattern}' matches no rule in {Path.GetFileName(file)}.");
                return false;
            case 1:
                return this.Show(file, matches[0].Path);
            default:
                this.workbench.SetStatus($"The path '{pattern}' matches {matches.Count} values. Pick one.");
                return this.PickFrom(file, matches, $"'{pattern}' matches {matches.Count} values: pick one");
        }
    }

    /// <summary>Asks which other rule of the document to edit, and opens it. Unsaved changes need the author's consent first.</summary>
    /// <returns><see langword="true"/> when another rule was opened.</returns>
    public bool Switch()
    {
        if (this.Session is not { } session)
        {
            this.workbench.SetStatus("No rule of a larger document is open. Press F2 to open one.");
            return false;
        }

        ToolResult<IReadOnlyList<DocumentNode>> outline = EmbeddedRuleSession.Outline(session.FilePath);
        if (!outline.TryGetValue(out IReadOnlyList<DocumentNode>? nodes, out EquatableArray<Diagnostic> diagnostics))
        {
            this.workbench.SetStatus(Describe(diagnostics));
            return false;
        }

        // The rules of the tree format, and the open rule when it is a string of rule text.
        DocumentNode[] rules = [.. nodes.Where(n => n.Kind == DocumentNodeKind.Rule || n.Path == session.RulePath)];
        if (rules.Length < 2)
        {
            this.workbench.SetStatus(
                $"{Path.GetFileName(session.FilePath)} has no other rule in the tree format to switch to."
            );
            return false;
        }

        if (this.workbench.Document.IsDirty && !this.workbench.Prompts.ConfirmDiscard("switch to another rule"))
        {
            return false;
        }

        return this.PickFrom(session.FilePath, rules, "Switch to another rule");
    }

    /// <summary>
    /// Writes the rule back into the document, changing only the rule's own span; every other byte of the file stays. The
    /// author confirms first, and the confirmation says what will change.
    /// </summary>
    /// <returns><see langword="true"/> when the file was written.</returns>
    public bool SaveInPlace()
    {
        return this.Save(WriteBackMode.Patch);
    }

    /// <summary>
    /// Writes the rule back by reading the whole document and writing it again. The author is warned first that YAML
    /// comments and the layout of the document may be lost, and the file is written only after the author agrees.
    /// </summary>
    /// <returns><see langword="true"/> when the file was written.</returns>
    public bool SaveRoundTrip()
    {
        return this.Save(WriteBackMode.RoundTrip);
    }

    private static string Describe(IEnumerable<Diagnostic> diagnostics)
    {
        Diagnostic? first = diagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
        return first is null ? "The document cannot be used." : $"{first.Code}: {first.Message}";
    }

    private static string Summary(
        EmbeddedRuleSession session,
        EmbeddedRuleSession.PreparedWrite write,
        IEnumerable<Diagnostic> diagnostics
    )
    {
        List<string> lines =
        [
            $"Write the rule at {session.RulePath} into {Path.GetFileName(session.FilePath)}.",
            KindText(write.Kind) + ".",
        ];
        lines.AddRange(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).Select(d => d.Message));
        if (write.ChangedOnDisk)
        {
            lines.Add("The rule in the file changed since you opened it. Writing replaces that version with yours.");
        }

        return string.Join('\n', lines);
    }

    private static string KindText(RulePatchKind kind)
    {
        return kind switch
        {
            RulePatchKind.Patched => "Only the changed values are replaced; comments and layout stay",
            RulePatchKind.RuleRewritten =>
                "The rule's own text is written again; comments and layout inside the rule may be lost, the rest of the file stays",
            RulePatchKind.DocumentRewritten => "The whole document is written again",
            _ => "No change",
        };
    }

    // Lists the entries (indented by depth), asks for one, and opens it.
    private bool PickFrom(string file, IReadOnlyList<DocumentNode> nodes, string title)
    {
        string[] items = [.. nodes.Select(n => $"{new string(' ', 2 * Math.Min(n.Depth, 8))}{n.Path}  {n.Label}")];
        return this.Dialogs.Choose(title, items) is { } index
            && index >= 0
            && index < nodes.Count
            && this.Show(file, nodes[index].Path);
    }

    // Reads the rule and puts it in the workbench in place of the document.
    private bool Show(string file, string rulePath)
    {
        ToolResult<EmbeddedRuleSession> opened = EmbeddedRuleSession.Open(file, rulePath);
        if (!opened.TryGetValue(out EmbeddedRuleSession? session, out EquatableArray<Diagnostic> diagnostics))
        {
            this.workbench.SetStatus(Describe(diagnostics));
            return false;
        }

        this.workbench.ReplaceDocument(
            session.CreateDocument(this.workbench.Settings),
            $"Editing {session.Label} as {Workbench.FormatName(session.RuleFormat)}. Ctrl+S writes only this rule back."
        );
        this.workbench.DocumentLabel = session.Label;
        this.workbench.SaveOverride = this.SaveInPlace;
        this.Session = session;
        return true;
    }

    private bool Save(WriteBackMode mode)
    {
        if (this.Session is not { } session)
        {
            this.workbench.SetStatus("No rule of a larger document is open. Press F2 to open one.");
            return false;
        }

        RuleDocument document = this.workbench.Document;
        if (!document.IsRuleCurrent)
        {
            this.workbench.SetStatus("Not saved: the rule text has errors. Fix them first, or undo to the last valid text.");
            return false;
        }

        // The warning comes before any work, so the author can stop a round trip that could lose comments.
        if (
            mode == WriteBackMode.RoundTrip
            && !this.Dialogs.Confirm(
                "Round-trip write-back",
                $"This reads all of {Path.GetFileName(session.FilePath)} and writes it again.\nYAML comments and the layout of the document may be lost.\nWrite it?",
                "Write whole document"
            )
        )
        {
            return false;
        }

        ToolResult<EmbeddedRuleSession.PreparedWrite> prepared = session.Prepare(document, mode, this.workbench.Settings);
        if (!prepared.TryGetValue(out EmbeddedRuleSession.PreparedWrite? write, out EquatableArray<Diagnostic> diagnostics))
        {
            this.workbench.SetStatus("Not saved: " + Describe(diagnostics));
            return false;
        }

        if (write.Kind == RulePatchKind.Unchanged)
        {
            document.MarkSaved();
            this.workbench.SetStatus($"Nothing to write: the rule equals the one in {Path.GetFileName(session.FilePath)}.");
            return true;
        }

        if (
            mode == WriteBackMode.Patch
            && !this.Dialogs.Confirm("Write the rule into the document", Summary(session, write, prepared.Diagnostics), "Write")
        )
        {
            return false;
        }

        ToolResult<string> written = session.Commit(write);
        if (!written.TryGetValue(out string? file, out diagnostics))
        {
            this.workbench.SetStatus("Not saved: " + Describe(diagnostics));
            return false;
        }

        document.MarkSaved();
        this.workbench.SetStatus($"Wrote {session.RulePath} into {file} ({KindText(write.Kind)}).");
        return true;
    }
}
