namespace TruthWeaver.Tool.Tui.Controllers;

using System.Runtime.CompilerServices;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Palette;
using TruthWeaver.Tool.Core.Tree;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The logic behind the Tree pane, with no view of its own: the lines of the tree, the selected node, the structural edits,
/// the palette and the clipboard. Each edit acts on <see cref="Workbench.SelectedNode"/>, goes through
/// <see cref="RuleTreeEditor"/>, and puts the result into the document as one undo step. An edit that the compiler refuses
/// changes nothing and shows the reason on the status line. When the text has <c>let</c> definitions, the tree marks the
/// uses and lists the definitions after the rule, and the edits keep the definitions.
/// </summary>
internal sealed class StructureController
{
    /// <summary>The first entry of the palette list: type the expression instead of picking one.</summary>
    public const string TypeExpression = "Type an expression (rule text, JSON or YAML)";

    /// <summary>The second entry of the palette list: search the ready-made predicates of the catalog.</summary>
    public const string SearchCatalog = "Search the predicate catalog (name, label, family, words)";

    /// <summary>The end of the line of a node that no saved case reaches. It is text, so it does not depend on the color.</summary>
    public const string NoCaseMark = "  [no case]";

    private static readonly ConditionalWeakTable<Workbench, StructureController> Controllers = [];

    // The short label of each catalog entry, for the line of a catalog predicate in a list.
    private static readonly Lazy<Dictionary<PaletteEntry, string>> CatalogLabels = new(() =>
        PredicateCatalog.All.ToDictionary(p => p.Entry, p => p.Label)
    );

    private readonly Workbench workbench;
    private readonly WorkbenchServices? services;
    private RuleTree? tree;
    private string? schemaNotice;

    /// <summary>Initializes a new instance of the <see cref="StructureController"/> class.</summary>
    /// <param name="workbench">The shared state.</param>
    /// <param name="services">The dialogs and the clipboard, or <see langword="null"/> for <see cref="WorkbenchServices.Current"/>.</param>
    public StructureController(Workbench workbench, WorkbenchServices? services = null)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        this.workbench = workbench;
        this.services = services;
        workbench.DocumentChanged += (_, _) =>
        {
            // A node that the new tree does not have can no longer be the target of an edit; the root is.
            this.tree = null;
            if (this.Tree is { } current && current.Find(workbench.SelectedNode) is null)
            {
                workbench.SelectNode("$");
            }
        };
    }

    /// <summary>Gets the tree of the last valid rule, or <see langword="null"/> when no text compiled yet.</summary>
    public RuleTree? Tree
    {
        get
        {
            if (this.tree is null || !ReferenceEquals(this.tree.Rule, this.workbench.Document.Rule))
            {
                CompiledRule<StubAssignment>? rule = this.workbench.Document.Rule;
                this.tree = rule is null ? null : RuleTree.Of(rule, this.workbench.Settings, this.Definitions());
            }

            return this.tree;
        }
    }

    /// <summary>Gets the line above the tree: the selected node, its literals and its note.</summary>
    public string Header
    {
        get
        {
            if (this.Tree is null)
            {
                return "No valid tree yet: the rule text has errors.";
            }

            RuleTreeNode node = this.SelectedNodeOrRoot();
            string literals = string.Join(", ", node.Literals.Select(l => $"{l.Key}={l.Value}"));
            string header = literals.Length == 0 ? node.Path : $"{node.Path}  {literals}";
            if (node.Note is { } note)
            {
                header += $"  // {note.ReplaceLineEndings(" ")}";
            }

            return this.workbench.Document.IsRuleCurrent ? header : CanonicalTextPane.StaleNotice + " " + header;
        }
    }

    /// <summary>
    /// Gets the lines of the tree: one for each node, indented by its depth. When the saved cases are measured, a node that
    /// no case reaches ends with <see cref="NoCaseMark"/>.
    /// </summary>
    public IReadOnlyList<string> Lines
    {
        get
        {
            if (this.Tree is not { } current)
            {
                return [];
            }

            CaseCoverage coverage = this.workbench.Coverage;
            EvaluationStepController stepping = EvaluationStepController.For(this.workbench);
            return
            [
                .. current.Nodes.Select(n =>
                    new string(' ', n.Depth * 2)
                    + n.Label
                    + this.BadgeOf(n)
                    + stepping.MarkOf(n.Path)
                    + (coverage.IsUncovered(n.Path) ? NoCaseMark : string.Empty)
                ),
            ];
        }
    }

    /// <summary>
    /// Gets the value of the selected node for the status line, such as <c>hasRole(role: "admin") = True</c>, or
    /// <see langword="null"/> when the selected node is not a term, no rule has compiled, the text has errors (the tree is then
    /// the last valid one and may not match the text), or the rule has no values yet.
    /// </summary>
    public string? SelectedValueText
    {
        get
        {
            // The committed document: reading Document would apply the pending text and end the pause.
            if (
                this.workbench.CommittedDocument.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)
                || this.Tree is null
                || this.SelectedNodeOrRoot() is not { Kind: RuleTreeNodeKind.Term } node
            )
            {
                return null;
            }

            return this.ValueTextOf(node) is { } value ? $"{node.Label} = {value}" : null;
        }
    }

    /// <summary>Gets the summary line of the coverage over the saved cases, or the reason there is none.</summary>
    public string CoverageSummary => this.workbench.Coverage.Summary;

    /// <summary>Gets the index of the selected node in <see cref="Lines"/>, or -1.</summary>
    public int SelectedIndex
    {
        get
        {
            IReadOnlyList<RuleTreeNode> nodes = this.Tree?.Nodes ?? [];
            for (int i = 0; i < nodes.Count; i++)
            {
                if (string.Equals(nodes[i].Path, this.workbench.SelectedNode, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return nodes.Count > 0 ? 0 : -1;
        }
    }

    private WorkbenchServices Services => this.services ?? WorkbenchServices.Current;

    /// <summary>Gets the controller of a workbench. The pane and the keys use the same controller.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <param name="services">The dialogs and the clipboard for a new controller, or <see langword="null"/> for <see cref="WorkbenchServices.Current"/>.</param>
    /// <returns>The controller.</returns>
    public static StructureController For(Workbench workbench, WorkbenchServices? services = null)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        return Controllers.GetValue(workbench, key => new StructureController(key, services));
    }

    /// <summary>
    /// Gets the value of the term at an offset of the rule text, for the status line, such as
    /// <c>hasRole(role: "admin") = True</c>. The cursor in a predicate call or in its arguments (a <c>from(...)</c> variable
    /// too) names that term.
    /// </summary>
    /// <param name="offset">The cursor offset in the text of the committed document.</param>
    /// <returns>
    /// The text, or <see langword="null"/> when the cursor is in no term, the text has errors, the rule is not rule text, the
    /// call is not a term of the rule, or no rule has compiled.
    /// </returns>
    public string? ValueTextAt(int offset)
    {
        // The committed document: reading Document would apply the pending text and end the pause.
        RuleDocument document = this.workbench.CommittedDocument;
        if (document.Format != RuleFormat.Text || document.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            return null;
        }

        AssignmentOutcome outcome = this.workbench.Outcome;
        if (!outcome.HasRule)
        {
            return null;
        }

        // The Core lookup turns the cursor into the term: the call at the offset, matched to a term without regard to spacing.
        return TermOutcomeLookup.FindBySpan(document.Text, outcome, new SourceSpan(offset, 0)) is { } term
            ? $"{term.Display} = {ValueTextOf(term)}"
            : null;
    }

    /// <summary>Selects the node at an index of <see cref="Lines"/>.</summary>
    /// <param name="index">The index.</param>
    public void SelectIndex(int index)
    {
        if (this.Tree is { } current && index >= 0 && index < current.Nodes.Count)
        {
            this.workbench.SelectNode(current.Nodes[index].Path);
        }
    }

    /// <summary>Gets the palette: the operators, the constants, the predicates of the schema file and those of the rule.</summary>
    /// <returns>The palette.</returns>
    public RulePalette Palette()
    {
        return PaletteController.For(this.workbench).Palette();
    }

    /// <summary>Asks for an operator from the palette and puts it in place of the operator of the selected node.</summary>
    /// <returns><see langword="true"/> when the rule changed.</returns>
    public bool ReplaceOperator()
    {
        if (!this.workbench.RequireCurrentRule("edit the tree"))
        {
            return false;
        }

        PaletteEntry[] operators = [.. this.Palette().Entries.Where(e => e.Source == PaletteSource.Operator)];
        int? choice = this.Services.Dialogs.Choose(
            "Replace the operator with",
            [.. operators.Select(o => o.Name.PadRight(12) + " " + o.Description)]
        );
        return choice is int index && this.Apply(e => e.ReplaceOperator(this.workbench.SelectedNode, operators[index].Name));
    }

    /// <summary>Wraps the selected node in <c>NOT</c>.</summary>
    /// <returns><see langword="true"/> when the rule changed.</returns>
    public bool WrapInNot()
    {
        return this.Edit(e => e.WrapInNot(this.workbench.SelectedNode));
    }

    /// <summary>Negates the selected node (see <see cref="RuleTreeEditor.Negate"/>).</summary>
    /// <returns><see langword="true"/> when the rule changed.</returns>
    public bool Negate()
    {
        return this.Edit(e => e.Negate(this.workbench.SelectedNode));
    }

    /// <summary>Asks for an expression from the palette and adds it as the last operand of the selected operator.</summary>
    /// <returns><see langword="true"/> when the rule changed.</returns>
    public bool InsertOperand()
    {
        if (!this.workbench.RequireCurrentRule("edit the tree") || this.PickExpression("Insert an operand") is not { } text)
        {
            return false;
        }

        return this.NoticeAfter(this.Apply(e => e.InsertOperand(this.workbench.SelectedNode, text)));
    }

    /// <summary>Deletes the selected node.</summary>
    /// <returns><see langword="true"/> when the rule changed.</returns>
    public bool Delete()
    {
        return this.Edit(e => e.Delete(this.workbench.SelectedNode));
    }

    /// <summary>Swaps the selected node with its neighbor.</summary>
    /// <param name="direction">1 for the next operand; -1 for the previous one.</param>
    /// <returns><see langword="true"/> when the rule changed.</returns>
    public bool Swap(int direction)
    {
        return this.Edit(e => e.Swap(this.workbench.SelectedNode, direction));
    }

    /// <summary>Asks for an expression from the palette and puts it in place of the selected node.</summary>
    /// <returns><see langword="true"/> when the rule changed.</returns>
    public bool ReplaceFromPalette()
    {
        if (!this.workbench.RequireCurrentRule("edit the tree") || this.PickExpression("Replace the node with") is not { } text)
        {
            return false;
        }

        return this.NoticeAfter(this.Apply(e => e.Replace(this.workbench.SelectedNode, text)));
    }

    /// <summary>Asks for a literal of the selected node and its new value, and changes it.</summary>
    /// <returns><see langword="true"/> when the rule changed.</returns>
    public bool ChangeLiteral()
    {
        if (!this.workbench.RequireCurrentRule("edit the tree"))
        {
            return false;
        }

        RuleTreeNode node = this.SelectedNodeOrRoot();
        if (node.Literals.Count == 0)
        {
            this.workbench.SetStatus("The selected node has no literal to change.");
            return false;
        }

        int? choice =
            node.Literals.Count == 1
                ? 0
                : this.Services.Dialogs.Choose("Change the literal", [.. node.Literals.Select(l => $"{l.Key} = {l.Value}")]);
        if (choice is not int index)
        {
            return false;
        }

        KeyValuePair<string, string> literal = node.Literals[index];
        string? value = this.Services.Dialogs.AskText(
            $"New value of {literal.Key} (JSON, or plain text for a string)",
            literal.Value
        );
        return value is not null && this.Apply(e => e.ChangeLiteral(node.Path, literal.Key, value));
    }

    /// <summary>Copies the selected node as rule text that compiles.</summary>
    /// <returns><see langword="true"/> when the clipboard took the text.</returns>
    public bool CopyNode()
    {
        return this.CopyText(this.workbench.SelectedNode, "the selected node");
    }

    /// <summary>Copies the whole rule as rule text that compiles.</summary>
    /// <returns><see langword="true"/> when the clipboard took the text.</returns>
    public bool CopyRule()
    {
        return this.CopyText("$", "the rule");
    }

    /// <summary>
    /// Puts the expression on the clipboard in place of the selected node. Text that is not a valid expression is rejected,
    /// and the status line shows its first diagnostic and their number.
    /// </summary>
    /// <returns><see langword="true"/> when the rule changed.</returns>
    public bool Paste()
    {
        if (!this.workbench.RequireCurrentRule("paste"))
        {
            return false;
        }

        if (!this.Services.Clipboard.TryGet(out string? text) || text.Trim().Length == 0)
        {
            this.workbench.SetStatus("Nothing to paste: the clipboard has no text, or is not available.");
            return false;
        }

        return this.Apply(e => e.Replace(this.workbench.SelectedNode, text), "Pasted onto the selected node.");
    }

    private static string Describe(RuleTreeEdit edit)
    {
        int errors = edit.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
        return errors > 1 ? $"{edit.Message} ({errors} errors)" : edit.Message;
    }

    private static string GroupLine(string family)
    {
        return ChoiceDialog.HeaderLine(family);
    }

    // Adds the groups that a palette search found to a list: a header line for each titled group, then a line for each entry.
    // The list of the dialog and the targets that go with it stay in step.
    private static void AddGroups(IEnumerable<PaletteGroup> groups, List<string> items, List<PaletteEntry?> targets)
    {
        foreach (PaletteGroup group in groups)
        {
            if (group.Title is { } title)
            {
                items.Add(GroupLine(title));
                targets.Add(null);
            }

            foreach (PaletteEntry entry in group.Entries)
            {
                items.Add(
                    CatalogLabels.Value.TryGetValue(entry, out string? label) ? entry.Display + " - " + label : entry.Display
                );
                targets.Add(entry);
            }
        }
    }

    // The indexes of a palette list that a search shows: the whole list for an empty search, otherwise the matches of the
    // palette search, each catalog match under its family line. The first two lines (typed expression, catalog search) are
    // not matches.
    private static List<int> ShownBy(
        RulePalette palette,
        IReadOnlyList<PaletteEntry?> targets,
        IReadOnlyList<string> items,
        string query
    )
    {
        if (query.Trim().Length == 0)
        {
            return [.. Enumerable.Range(0, items.Count)];
        }

        Dictionary<PaletteEntry, int> entryIndex = [];
        Dictionary<string, int> headerIndex = [];
        for (int i = 2; i < targets.Count; i++)
        {
            if (targets[i] is { } entry)
            {
                entryIndex[entry] = i;
            }
            else
            {
                headerIndex[items[i]] = i;
            }
        }

        List<int> shown = [];
        foreach (PaletteGroup group in palette.Search(query))
        {
            if (group.Title is { } title && headerIndex.TryGetValue(GroupLine(title), out int header))
            {
                shown.Add(header);
            }

            shown.AddRange(group.Entries.Select(e => entryIndex[e]));
        }

        return shown;
    }

    // The value of a term, as the words of the value: True, False or Unknown. A term whose from(...) reference has no data
    // is Unknown, and the text says why (the fault message of the evaluation, or that no data arrived yet).
    private static string ValueTextOf(TermOutcome term)
    {
        if (term.Key is not null)
        {
            return term.Value.ToString();
        }

        string? fault = term.Faults.Select(f => f.Exception.Message.Trim()).FirstOrDefault();
        return $"Unknown: {fault ?? "no data yet"}";
    }

    // The value text of the term that a tree node stands for, or null when no rule has compiled or the node has no term.
    private string? ValueTextOf(RuleTreeNode node)
    {
        AssignmentOutcome outcome = this.workbench.Outcome;
        if (
            !outcome.HasRule
            || this.Tree is not { } current
            || TermOutcomeLookup.FindByPath(current, outcome, node.Path) is not { } term
        )
        {
            return null;
        }

        return ValueTextOf(term);
    }

    // The badge at the end of the line of a term in the tree. It is text, so it does not depend on the color. Other nodes
    // have no badge.
    private string BadgeOf(RuleTreeNode node)
    {
        return node.Kind == RuleTreeNodeKind.Term && this.ValueTextOf(node) is { } value ? $"  [{value}]" : string.Empty;
    }

    private bool Edit(Func<RuleTreeEditor, RuleTreeEdit> edit)
    {
        return this.workbench.RequireCurrentRule("edit the tree") && this.Apply(edit);
    }

    // Runs an edit on the rule of the document and puts the result in. A refused edit leaves the document as it was.
    private bool Apply(Func<RuleTreeEditor, RuleTreeEdit> edit, string? done = null)
    {
        RuleTreeEdit result = edit(
            new RuleTreeEditor(this.workbench.Document.Rule!, this.workbench.Settings, this.Definitions())
        );
        if (!result.Succeeded)
        {
            this.workbench.SetStatus(Describe(result));
            return false;
        }

        if (!this.workbench.ApplyEdit(result, done ?? result.Message + " Ctrl+Z undoes it."))
        {
            return false;
        }

        this.workbench.SelectNode(result.Path ?? "$");
        return true;
    }

    private bool CopyText(string path, string what)
    {
        if (this.Tree is not { } current)
        {
            this.workbench.SetStatus("Nothing to copy: no text of the rule compiled yet.");
            return false;
        }

        ToolResult<string> text = current.TextOf(path);
        if (!text.Succeeded)
        {
            Diagnostic? first = text.Diagnostics.Count > 0 ? text.Diagnostics[0] : null;
            this.workbench.SetStatus($"Cannot copy {what}: {first?.Message ?? "it does not compile on its own."}");
            return false;
        }

        if (!this.Services.Clipboard.TrySet(text.Value))
        {
            this.workbench.SetStatus("Not copied: the clipboard is not available in this terminal.");
            return false;
        }

        this.workbench.SetStatus($"Copied {what} as rule text.");
        return true;
    }

    // Asks for an expression: a constant or a predicate of the palette (with a form for its arguments), or typed text. The
    // list holds the typed expression, the catalog search, the constants and the predicates of the schema file and of the
    // rule, and then the ready-made predicates of the catalog in family groups.
    private string? PickExpression(string title)
    {
        this.schemaNotice = null;
        RulePalette palette = this.Palette();
        List<string> items = [TypeExpression, SearchCatalog];
        List<PaletteEntry?> targets = [null, null];
        AddGroups(palette.Search(string.Empty), items, targets);

        PaletteEntry? picked = null;
        while (picked is null)
        {
            int? choice = this.Services.Dialogs.ChooseFiltered(title, items, query => ShownBy(palette, targets, items, query));
            switch (choice)
            {
                case null:
                    return null;
                case 0:
                    return this.Services.Dialogs.AskText(TypeExpression, string.Empty);
                case 1:
                    picked = this.SearchCatalogEntry();
                    if (picked is null)
                    {
                        return null;
                    }

                    break;
                default:
                    // The dialog never returns a group line; a null target is only a guard.
                    picked = targets[choice.Value];
                    break;
            }
        }

        PaletteEntry entry = picked;
        List<string> values = [];
        foreach (PaletteArgument argument in entry.Arguments)
        {
            string optional = argument.Required ? string.Empty : ", optional: empty leaves it out";
            if (
                this.Services.Dialogs.AskText(
                    $"{entry.Name}: {argument.Name} ({argument.Kind}{optional})",
                    argument.Default ?? string.Empty
                )
                is not { } value
            )
            {
                return null;
            }

            values.Add(value);
        }

        ToolResult<string> filled = RulePalette.Fill(entry, values, this.workbench.Settings);
        this.schemaNotice = filled.Succeeded ? this.SchemaGap(entry) : null;
        if (!filled.Succeeded)
        {
            this.workbench.SetStatus($"Not applied: {filled.FailureSummary("the call does not compile.")}");
            return null;
        }

        return filled.Value;
    }

    // Asks for words and lists the predicates of the catalog that match, in family groups. Returns the picked predicate, or
    // null when the author cancels or nothing matches.
    private PaletteEntry? SearchCatalogEntry()
    {
        if (this.Services.Dialogs.AskText("Search the predicate catalog", string.Empty) is not { } query)
        {
            return null;
        }

        IReadOnlyList<PaletteGroup> found = this.Palette().Search(query, e => e.Source == PaletteSource.Catalog);
        if (found.Count == 0)
        {
            this.workbench.SetStatus($"No predicate of the catalog matches '{query.Trim()}'.");
            return null;
        }

        List<string> items = [];
        List<PaletteEntry?> targets = [];
        AddGroups(found, items, targets);

        while (this.Services.Dialogs.Choose($"Catalog: {query.Trim()}", items) is int choice)
        {
            // The dialog never returns a group line; the check is only a guard.
            if (targets[choice] is { } entry)
            {
                return entry;
            }
        }

        return null;
    }

    // The notice for a catalog predicate that the schema file of the settings does not declare. The file is never edited: the
    // predicate goes into the rule, and the schema validation level reports it. Null when no schema file is set or readable.
    private string? SchemaGap(PaletteEntry entry)
    {
        if (
            entry.Source != PaletteSource.Catalog
            || this.workbench.Settings.SchemaFile is null
            || PaletteController.For(this.workbench).Schema() is not { } declared
        )
        {
            return null;
        }

        return declared.Predicates.Any(p => p.Name == entry.Name)
            ? null
            : $"The schema file does not declare '{entry.Name}'; the schema validation level will flag it.";
    }

    // Adds the notice of the last pick to the status line, after the edit has put its own message there.
    private bool NoticeAfter(bool changed)
    {
        if (changed && this.schemaNotice is { } notice)
        {
            this.workbench.SetStatus($"{this.workbench.Status} {notice}");
        }

        this.schemaNotice = null;
        return changed;
    }

    // The definitions of the text, only when the text compiled: the tree and the edits start from that rule.
    private RuleDefinitions? Definitions()
    {
        return PaletteController.For(this.workbench).Definitions();
    }

    private RuleTreeNode SelectedNodeOrRoot()
    {
        RuleTree current = this.Tree!;
        return current.Find(this.workbench.SelectedNode) ?? current.Nodes[0];
    }
}
