namespace TruthWeaver.Tool.Tui;

using System.Runtime.CompilerServices;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core;

/// <summary>
/// The logic behind the Tree pane, with no view of its own: the lines of the tree, the selected node, the structural edits,
/// the palette and the clipboard. Each edit acts on <see cref="Workbench.SelectedNode"/>, goes through
/// <see cref="RuleTreeEditor"/>, and puts the result into the document as one undo step. An edit that the compiler refuses
/// changes nothing and shows the reason on the status line.
/// </summary>
internal sealed class StructureController
{
    /// <summary>The first entry of the palette list: type the expression instead of picking one.</summary>
    public const string TypeExpression = "Type an expression (rule text, JSON or YAML)";

    private static readonly ConditionalWeakTable<Workbench, StructureController> Controllers = [];

    private readonly Workbench workbench;
    private readonly WorkbenchServices? services;
    private RuleTree? tree;
    private PredicateSchemaFile? schema;
    private bool schemaRead;

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
                this.tree = rule is null ? null : RuleTree.Of(rule, this.workbench.Settings);
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

    /// <summary>Gets the lines of the tree: one for each node, indented by its depth.</summary>
    public IReadOnlyList<string> Lines =>
        this.Tree is { } current ? [.. current.Nodes.Select(n => new string(' ', n.Depth * 2) + n.Label)] : [];

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
        return RulePalette.Create(this.workbench.Document.Rule, this.Schema());
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

        return this.Apply(e => e.InsertOperand(this.workbench.SelectedNode, text));
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

        return this.Apply(e => e.Replace(this.workbench.SelectedNode, text));
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

    private bool Edit(Func<RuleTreeEditor, RuleTreeEdit> edit)
    {
        return this.workbench.RequireCurrentRule("edit the tree") && this.Apply(edit);
    }

    // Runs an edit on the rule of the document and puts the result in. A refused edit leaves the document as it was.
    private bool Apply(Func<RuleTreeEditor, RuleTreeEdit> edit, string? done = null)
    {
        RuleTreeEdit result = edit(new RuleTreeEditor(this.workbench.Document.Rule!, this.workbench.Settings));
        if (!result.Succeeded)
        {
            this.workbench.SetStatus(Describe(result));
            return false;
        }

        if (!this.workbench.ReplaceRule(result.Rule, done ?? result.Message + " Ctrl+Z undoes it."))
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

    // Asks for an expression: a constant or a predicate of the palette (with a form for its arguments), or typed text.
    private string? PickExpression(string title)
    {
        PaletteEntry[] entries = [.. this.Palette().Entries.Where(e => e.Source != PaletteSource.Operator)];
        int? choice = this.Services.Dialogs.Choose(title, [TypeExpression, .. entries.Select(e => e.Display)]);
        if (choice is not int index)
        {
            return null;
        }

        if (index == 0)
        {
            return this.Services.Dialogs.AskText(TypeExpression, string.Empty);
        }

        PaletteEntry entry = entries[index - 1];
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
        if (!filled.Succeeded)
        {
            Diagnostic? first = filled.Diagnostics.FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
            this.workbench.SetStatus(
                $"Not applied: {(first is null ? "the call does not compile." : $"{first.Code}: {first.Message}")}"
            );
            return null;
        }

        return filled.Value;
    }

    private RuleTreeNode SelectedNodeOrRoot()
    {
        RuleTree current = this.Tree!;
        return current.Find(this.workbench.SelectedNode) ?? current.Nodes[0];
    }

    // The schema file of the settings, read once. A file that cannot be read gives no predicates; the validation panel
    // reports its problems.
    private PredicateSchemaFile? Schema()
    {
        if (!this.schemaRead)
        {
            this.schemaRead = true;
            if (this.workbench.Settings.SchemaFile is { } path)
            {
                this.schema = PredicateSchemaFile.Load(path).Value;
            }
        }

        return this.schema;
    }
}
