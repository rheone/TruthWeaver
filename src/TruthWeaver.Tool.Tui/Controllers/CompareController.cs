namespace TruthWeaver.Tool.Tui.Controllers;

using System.Runtime.CompilerServices;
using TruthWeaver.Analysis;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The logic behind the Compare pane, with no view of its own: it holds a second rule beside the open rule, loaded from a
/// file or from the clipboard, and shows how the second rule relates to the open rule (equivalent, tighter, looser or
/// unrelated, from <see cref="RuleImplication"/>) and which pairs of values the two rules can give for the same assignment
/// (<see cref="RuleOverlap"/>), each with an example assignment. The second rule is read-only: no key edits it, and it
/// never changes the open document.
/// </summary>
internal sealed class CompareController
{
    /// <summary>The text of the pane before a second rule is loaded.</summary>
    public const string Idle =
        "No second rule yet. Press o here, or Ctrl+F9 anywhere, to load one from a file or the clipboard.";

    private static readonly ConditionalWeakTable<Workbench, CompareController> Controllers = [];

    private readonly Workbench workbench;
    private readonly WorkbenchServices? services;
    private RuleDocument? other;
    private string origin = string.Empty;
    private IReadOnlyList<Diagnostic> loadDiagnostics = [];

    /// <summary>Initializes a new instance of the <see cref="CompareController"/> class.</summary>
    /// <param name="workbench">The shared state.</param>
    /// <param name="services">The dialogs and the clipboard, or <see langword="null"/> for <see cref="WorkbenchServices.Current"/>.</param>
    public CompareController(Workbench workbench, WorkbenchServices? services = null)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        this.workbench = workbench;
        this.services = services;
        this.Lines = [new CompareLine(Idle)];
        workbench.DocumentChanged += (_, _) => this.Refresh();
    }

    /// <summary>Occurs after a second rule is loaded or closed, and after a change of the open rule changed the comparison.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets the lines of the pane.</summary>
    public IReadOnlyList<CompareLine> Lines { get; private set; }

    private WorkbenchServices Services => this.services ?? WorkbenchServices.Current;

    /// <summary>Gets the controller of a workbench. The pane and the keys use the same controller.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <param name="services">The dialogs and the clipboard for a new controller, or <see langword="null"/> for <see cref="WorkbenchServices.Current"/>.</param>
    /// <returns>The controller.</returns>
    public static CompareController For(Workbench workbench, WorkbenchServices? services = null)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        return Controllers.GetValue(workbench, key => new CompareController(key, services));
    }

    /// <summary>Asks where the second rule comes from (the clipboard or a file) and loads it.</summary>
    /// <returns><see langword="true"/> when a second rule was read, even one that does not compile.</returns>
    public bool Open()
    {
        int? source = this.Services.Dialogs.Choose("Load the second rule from", ["The clipboard", "A file"]);
        if (source is null)
        {
            return false;
        }

        if (source == 0)
        {
            return this.FromClipboard();
        }

        return this.workbench.Prompts.AskOpenPath() is { } path && this.FromFile(path);
    }

    /// <summary>Loads the clipboard text as the second rule. Text that does not compile loads too, and the pane shows its diagnostics.</summary>
    /// <returns><see langword="true"/> when the clipboard gave text.</returns>
    public bool FromClipboard()
    {
        if (!this.Services.Clipboard.TryGet(out string? text) || text.Trim().Length == 0)
        {
            this.workbench.SetStatus("The clipboard has no text, or it is not available here.");
            return false;
        }

        ToolResult<ImportedRule> imported = RuleImport.FromText(
            text,
            RuleImport.ClipboardOrigin,
            format: null,
            this.workbench.Settings,
            requireValid: false
        );
        this.Take(imported.Value?.Document, imported.Diagnostics, "the clipboard");
        return true;
    }

    /// <summary>Loads a file as the second rule. A file that does not compile loads too, and the pane shows its diagnostics.</summary>
    /// <param name="path">The file.</param>
    /// <returns><see langword="true"/> when the file was read.</returns>
    public bool FromFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        ToolResult<RuleDocument> opened = RuleDocument.Open(path, this.workbench.Settings);
        this.Take(opened.Value, opened.Diagnostics, path);
        return opened.Value is not null;
    }

    /// <summary>Drops the second rule.</summary>
    public void Close()
    {
        this.other = null;
        this.loadDiagnostics = [];
        this.origin = string.Empty;
        this.workbench.SetStatus("Closed the second rule.");
        this.Refresh();
    }

    /// <summary>Puts an example assignment of the pane into the Assign pane, in place of its term values. The variables stay.</summary>
    /// <param name="line">The line of the pane.</param>
    /// <returns><see langword="true"/> when the values were set.</returns>
    public bool LoadExample(CompareLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        if (line.Example is not { } example)
        {
            this.workbench.SetStatus("Select a line that has an example assignment.");
            return false;
        }

        IReadOnlyList<TermAssignment> terms = this.workbench.Outcome.Terms;
        List<(string Key, TruthValue Value)> sets = [];
        foreach ((string term, TruthValue value) in example)
        {
            TermAssignment? known = terms.FirstOrDefault(t => t.Key == term || t.Display == term);
            if (known is not null && known.Key is null)
            {
                this.workbench.SetStatus(
                    $"Not loaded: '{term}' takes its value from a from(...) reference, so it cannot be set here."
                );
                return false;
            }

            sets.Add((known?.Key ?? term, value));
        }

        this.workbench.Assignment.ClearValues();
        foreach ((string key, TruthValue value) in sets)
        {
            this.workbench.Assignment.Set(key, value);
        }

        this.workbench.NotifyAssignmentChanged();
        this.workbench.SetStatus("Loaded the example into the Assign pane: " + RewriteController.Assignment(example) + ".");
        return true;
    }

    private static string Relation(RuleRelation relation)
    {
        return relation switch
        {
            RuleRelation.Equivalent => "equivalent: the same assignments satisfy both",
            RuleRelation.Tighter => "tighter: it is satisfied by fewer assignments",
            RuleRelation.Looser => "looser: it is satisfied by more assignments",
            RuleRelation.Overlapping => "unrelated: some assignments satisfy both, and each rule has some the other has not",
            RuleRelation.Disjoint => "unrelated: no assignment satisfies both",
            _ => "undecided",
        };
    }

    private static void AddExample(List<CompareLine> lines, string label, IReadOnlyDictionary<string, TruthValue>? example)
    {
        if (example is not null)
        {
            lines.Add(new CompareLine($"  {label}: {RewriteController.Assignment(example)}", example));
        }
    }

    private void Take(RuleDocument? document, IEnumerable<Diagnostic> diagnostics, string from)
    {
        this.other = document;
        this.origin = from;
        this.loadDiagnostics = document is null ? [.. diagnostics] : [];
        bool compiles = document is { IsRuleCurrent: true, Rule: not null };
        this.workbench.SetStatus(
            compiles
                ? $"Loaded the second rule from {from}. It is read-only."
                : $"The second rule from {from} does not compile: see the Compare pane."
        );
        this.Refresh();
    }

    private void Refresh()
    {
        this.Lines = this.other is null && this.loadDiagnostics.Count == 0 ? [new CompareLine(Idle)] : this.Build();
        this.Changed?.Invoke(this, EventArgs.Empty);
    }

    private List<CompareLine> Build()
    {
        List<CompareLine> lines =
        [
            new(
                $"Second rule (read-only) from {this.origin}   o: load another   Enter: load the example into Assign   x: close"
            ),
        ];

        if (this.other is not { IsRuleCurrent: true, Rule: { } second })
        {
            IEnumerable<Diagnostic> problems = this.other?.Diagnostics ?? this.loadDiagnostics;
            lines.Add(new CompareLine("The second rule does not compile, so there is no comparison. Diagnostics:"));
            lines.AddRange(
                problems.Select(d => new CompareLine($"  {d.Code} {d.Severity.ToString().ToLowerInvariant()}: {d.Message}"))
            );
            return lines;
        }

        if (!this.workbench.Document.IsRuleCurrent || this.workbench.Document.Rule is not { } first)
        {
            lines.Add(new CompareLine("The open rule has errors, so there is no comparison. Fix them first, or undo."));
            return lines;
        }

        int cap = this.workbench.Settings.MaxAnalysisTerms;
        RuleRelationResult relation = RuleImplication.Compare(first, second, cap);
        RuleOverlapResult overlap = RuleOverlap.Compare(first, second, cap);

        lines.Add(new CompareLine("Open rule:   " + first.CanonicalText));
        lines.Add(new CompareLine("Second rule: " + second.CanonicalText));
        lines.Add(
            new CompareLine(
                relation.Relation == RuleRelation.Undecided
                    ? "The second rule is undecided: " + relation.Reason
                    : "The second rule is " + Relation(relation.Relation)
            )
        );
        AddExample(lines, "Satisfies the open rule only", relation.SatisfiesOnlyBefore);
        AddExample(lines, "Satisfies the second rule only", relation.SatisfiesOnlyAfter);

        if (overlap.Outcome == RuleOverlapOutcome.Undecided)
        {
            lines.Add(new CompareLine("Overlap: undecided. " + overlap.Reason));
            return lines;
        }

        lines.Add(
            new CompareLine(
                "Overlap: both can be True: "
                    + (overlap.CanOverlap ? "yes" : "no")
                    + ". One True and the other not: "
                    + (overlap.CanConflict ? "yes" : "no")
            )
        );
        foreach (JointOutcome pair in overlap.Reachable.Where(p => p.First != p.Second))
        {
            lines.Add(
                new CompareLine(
                    $"  Open rule {pair.First}, second rule {pair.Second}: {RewriteController.Assignment(pair.Example)}",
                    pair.Example
                )
            );
        }

        return lines;
    }
}
