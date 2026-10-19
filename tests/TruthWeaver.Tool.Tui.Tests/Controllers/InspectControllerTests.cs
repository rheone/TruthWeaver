namespace TruthWeaver.Tool.Tui.Tests.Controllers;

using System.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TruthWeaver.Abstractions;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Tui.Commands;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Predicates;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>Tests of the inspect text: what the key and the hover say about each part of a rule.</summary>
[Collection(TerminalCollection.Name)]
public sealed class InspectControllerTests
{
    /// <summary>
    /// An operator says its name and arity, the description of the library and the value it returned now. The description is
    /// the one of the operator table, so it is not written twice.
    /// </summary>
    [Fact]
    public void DescribeNode_Operator_GivesNameDescriptionAndValue_Test()
    {
        string text = Describe(
            "a AND b",
            "$",
            workbench =>
            {
                workbench.Assignment.Set("a", TruthValue.True);
                workbench.Assignment.Set("b", TruthValue.True);
                workbench.NotifyAssignmentChanged();
            }
        );

        Assert.Contains("Operator AND (2 operands)", text, StringComparison.Ordinal);
        Assert.Contains("True iff every operand is true", text, StringComparison.Ordinal);
        Assert.Contains("Value now: True", text, StringComparison.Ordinal);
    }

    /// <summary>A node that short-circuiting never reached says <c>skipped</c> in words.</summary>
    [Fact]
    public void DescribeNode_SkippedOperand_SaysSkipped_Test()
    {
        string text = Describe(
            "a AND b",
            "$.operands[1]",
            workbench =>
            {
                workbench.Assignment.Set("a", TruthValue.False);
                workbench.NotifyAssignmentChanged();
            }
        );

        Assert.Contains("Value now: skipped (short-circuited)", text, StringComparison.Ordinal);
    }

    /// <summary><c>IMPLIES</c> carries its truth table.</summary>
    [Fact]
    public void DescribeNode_Implies_ShowsTheTruthTable_Test()
    {
        string text = Describe("a IMPLIES b", "$");

        Assert.Contains("Truth table", text, StringComparison.Ordinal);
        Assert.Contains("      U   T  U  U", text, StringComparison.Ordinal);
    }

    /// <summary><c>If</c> says what an Unknown condition does.</summary>
    [Fact]
    public void DescribeNode_If_ShowsWhatAnUnknownConditionDoes_Test()
    {
        string text = Describe("If(a, b, c)", "$");

        Assert.Contains("condition Unknown ->", text, StringComparison.Ordinal);
    }

    /// <summary>A cardinality operator gives its threshold and how many operands are True, False and Unknown now.</summary>
    [Fact]
    public void DescribeNode_CardinalityOperator_CountsTheOperands_Test()
    {
        string text = Describe(
            "AtLeast(2, a, b, c)",
            "$",
            workbench =>
            {
                workbench.Assignment.Set("a", TruthValue.True);
                workbench.Assignment.Set("b", TruthValue.False);
                workbench.NotifyAssignmentChanged();
            }
        );

        Assert.Contains("Threshold: k=2", text, StringComparison.Ordinal);
        Assert.Contains("Operands now: 1 True, 1 False, 1 Unknown, 0 skipped", text, StringComparison.Ordinal);
    }

    /// <summary>An external operator carries a mark.</summary>
    [Fact]
    public void DescribeNode_ExternalOperator_IsMarked_Test()
    {
        string text = Describe("IsUnknown(a)", "$");

        Assert.Contains("External operator", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// A predicate of the catalog gives its description, the names and kinds of its arguments, what it answers for a null
    /// value and its value now.
    /// </summary>
    [Fact]
    public void DescribeNode_CatalogPredicate_GivesArgumentsAndNullBehavior_Test()
    {
        CatalogPredicate predicate = PredicateCatalog.All.First(p => p.TakesNullBehavior && p.Entry.Arguments.Count > 0);
        string rule = InsertItems.PredicateText(predicate.Entry, ToolSettings.Default).Value!;

        string text = Describe(rule, "$");

        Assert.Contains($"Predicate {predicate.Entry.Name} ({predicate.Family})", text, StringComparison.Ordinal);
        Assert.Contains($"Arguments: {predicate.Entry.Arguments[0].Name}:", text, StringComparison.Ordinal);
        Assert.Contains("Null value: answers Unknown unless the host chose False.", text, StringComparison.Ordinal);
        Assert.Contains("Value now:", text, StringComparison.Ordinal);
    }

    /// <summary>A variable that reads from a source shows the source, the path and that no data resolved it yet.</summary>
    [Fact]
    public void DescribeNode_FromVariable_ShowsSourcePathAndFailure_Test()
    {
        string text = Describe("isSet(value: from(\"users\", \"$.name\"))", "$");

        Assert.Contains("Variable from source \"users\", path \"$.name\"", text, StringComparison.Ordinal);
        Assert.Contains("Not resolved:", text, StringComparison.Ordinal);
    }

    /// <summary>A constant says which value its letter stands for.</summary>
    [Fact]
    public void DescribeNode_ConstantLetter_SaysWhichValueItStandsFor_Test()
    {
        string text = Describe("U", "$");

        Assert.Contains("U stands for Unknown", text, StringComparison.Ordinal);
        Assert.Contains("Value now: Unknown", text, StringComparison.Ordinal);
    }

    /// <summary>A definition gives its body; a use gives the parameters and the body of the definition it uses.</summary>
    [Fact]
    public void DescribeNode_DefinitionAndUse_GiveTheBodyAndTheParameters_Test()
    {
        const string Rule = "let both(first, second) = first AND second; both(a, b)";
        ScriptedServices services = new();
        using WorkbenchHarness harness = new(Rule, [new RuleTextPane(), new TreePane(services.Services)]);
        InspectController inspect = InspectController.For(harness.Workbench);
        Core.Tree.RuleTree tree = StructureController.For(harness.Workbench).Tree!;
        string definition = tree.Nodes.Single(n => n.Kind == Core.Tree.RuleTreeNodeKind.Definition).Path;

        string text = inspect.DescribeNode(definition)!;

        Assert.Contains("Definition both(first, second)", text, StringComparison.Ordinal);
        Assert.Contains("Body: ", text, StringComparison.Ordinal);
        Assert.Contains("Used 1 time in the rule.", text, StringComparison.Ordinal);
    }

    /// <summary>A node that the tree does not have gives no text.</summary>
    [Fact]
    public void DescribeNode_UnknownPath_GivesNothing_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new TreePane(services.Services)]);

        Assert.Null(InspectController.For(harness.Workbench).DescribeNode("$.operands[9]"));
    }

    /// <summary>An operator word in the editor text gets the description of the library and, when it is the only one, its value.</summary>
    [Fact]
    public void DescribeOffset_OperatorWord_GivesTheDescriptionAndTheValue_Test()
    {
        string text = DescribeAt("a AND b", "AND");

        Assert.Contains("Operator AND", text, StringComparison.Ordinal);
        Assert.Contains("True iff every operand is true", text, StringComparison.Ordinal);
        Assert.Contains("Value now:", text, StringComparison.Ordinal);
    }

    /// <summary>A symbol of an operator is described as the operator it spells.</summary>
    [Fact]
    public void DescribeOffset_OperatorSymbol_IsDescribedAsTheOperator_Test()
    {
        string text = DescribeAt("a && b", "&&");

        Assert.Contains("True iff every operand is true", text, StringComparison.Ordinal);
    }

    /// <summary>A predicate name in the editor text gets the facts of the catalog and the value of the terms that use it.</summary>
    [Fact]
    public void DescribeOffset_PredicateName_GivesCatalogFactsAndValue_Test()
    {
        CatalogPredicate predicate = PredicateCatalog.All.First(p => p.TakesNullBehavior && p.Entry.Arguments.Count > 0);
        string rule = InsertItems.PredicateText(predicate.Entry, ToolSettings.Default).Value!;

        string text = DescribeAt(rule, predicate.Entry.Name);

        Assert.Contains($"Predicate {predicate.Entry.Name} ({predicate.Family})", text, StringComparison.Ordinal);
        Assert.Contains("Value now:", text, StringComparison.Ordinal);
    }

    /// <summary>A name that a <c>let</c> defines is described as the definition, with its body.</summary>
    [Fact]
    public void DescribeOffset_DefinitionName_GivesTheBody_Test()
    {
        string text = DescribeAt("let both(first, second) = first AND second; both(a, b)", "both(a");

        Assert.Contains("Definition both(first, second)", text, StringComparison.Ordinal);
        Assert.Contains("Body: ", text, StringComparison.Ordinal);
    }

    /// <summary>A parameter of a definition says whose parameter it is.</summary>
    [Fact]
    public void DescribeOffset_Parameter_NamesTheDefinition_Test()
    {
        string text = DescribeAt("let both(first, second) = first AND second; both(a, b)", "second;");

        Assert.Contains("Parameter second of definition both", text, StringComparison.Ordinal);
    }

    /// <summary>A constant in the editor text says which value it stands for.</summary>
    [Fact]
    public void DescribeOffset_Constant_SaysWhichValueItStandsFor_Test()
    {
        string text = DescribeAt("a AND T", "T");

        Assert.Contains("T stands for True", text, StringComparison.Ordinal);
    }

    /// <summary>A note shows its full text.</summary>
    [Fact]
    public void DescribeOffset_Note_ShowsTheFullText_Test()
    {
        string text = DescribeAt("a AND b // a long reason that the editor line might cut", "reason");

        Assert.StartsWith("Note", text, StringComparison.Ordinal);
        Assert.Contains("a long reason that the editor line might cut", text, StringComparison.Ordinal);
    }

    /// <summary>The span of a diagnostic gives the code, the full message and the documentation page.</summary>
    [Fact]
    public void DescribeOffset_DiagnosticSpan_GivesCodeMessageAndPage_Test()
    {
        using WorkbenchHarness harness = new("a AND", [new RuleTextPane()]);
        Diagnostics.Diagnostic error = harness.Workbench.Document.Diagnostics.First(d =>
            d.Severity == Diagnostics.DiagnosticSeverity.Error
        );

        string text = InspectController.For(harness.Workbench).DescribeOffset(error.Span.Start)!;

        Assert.Contains(error.Code, text, StringComparison.Ordinal);
        Assert.Contains(error.Message, text, StringComparison.Ordinal);
        Assert.Contains("docs/diagnostics.md", text, StringComparison.Ordinal);
    }

    /// <summary>The root of the tree lists the findings of the analyzer that name no node, such as an operand that cannot matter.</summary>
    [Fact]
    public void DescribeNode_RootWithAFinding_ListsTheFinding_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a OR T", [new RuleTextPane(), new TreePane(services.Services)]);
        Diagnostics.Diagnostic[] findings =
        [
            .. harness.Workbench.Document.Diagnostics.Where(d =>
                d.Severity != Diagnostics.DiagnosticSeverity.Error && d.Path is null
            ),
        ];
        Assert.NotEmpty(findings);

        string text = InspectController.For(harness.Workbench).DescribeNode("$")!;

        Assert.Contains("Findings:", text, StringComparison.Ordinal);
        Assert.Contains(findings[0].Code, text, StringComparison.Ordinal);
    }

    /// <summary>
    /// The inspect key on a node of the tree and the hover on its line show the same text, because both ask the same
    /// provider.
    /// </summary>
    [Fact]
    public void Inspect_TreeNode_KeyAndHoverShowTheSameText_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new TreePane(services.Services)]);
        harness.Workbench.SelectNode("$");
        TreePaneView tree = Assert.IsType<TreePaneView>(harness.Window.ActivePaneViews[PaneSlot.Side]);
        tree.List.SetFocus();

        harness.Press(InspectCommands.InspectKey);
        string byKey = harness.Window.InspectText;
        harness.Window.HideInspect();
        Point onFirstLine = tree.List.ViewportToScreen(new Point(0, 0));
        bool shown = harness.Window.InspectAt(tree.List, onFirstLine);

        Assert.True(shown);
        Assert.NotEmpty(byKey);
        Assert.Equal(byKey, harness.Window.InspectText);
    }

    /// <summary>The inspect key and the hover show the same text for the same place of the editor text.</summary>
    [Fact]
    public void Inspect_EditorText_KeyAndHoverShowTheSameText_Test()
    {
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane()]);
        RuleEditorView editor = Assert.IsType<RuleEditorView>(harness.Window.ActivePaneViews[PaneSlot.Editor]);
        editor.MoveTo(3);

        harness.Press(InspectCommands.InspectKey);
        string byKey = harness.Window.InspectText;
        harness.Window.HideInspect();
        bool shown = harness.Window.InspectAt(editor, editor.ViewportToScreen(new Point(3, 0)));

        Assert.True(shown);
        Assert.Contains("Operator AND", byKey, StringComparison.Ordinal);
        Assert.Equal(byKey, harness.Window.InspectText);
    }

    /// <summary>Esc closes the box.</summary>
    [Fact]
    public void Inspect_Esc_ClosesTheBox_Test()
    {
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane()]);
        RuleEditorView editor = Assert.IsType<RuleEditorView>(harness.Window.ActivePaneViews[PaneSlot.Editor]);
        editor.MoveTo(3);

        harness.Press(InspectCommands.InspectKey, Terminal.Gui.Input.Key.Esc);

        Assert.Empty(harness.Window.InspectText);
    }

    /// <summary>A place with nothing to describe leaves the box closed and says why on the status line.</summary>
    [Fact]
    public void Inspect_PlaceWithNothing_SaysSoOnTheStatusLine_Test()
    {
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane()]);
        RuleEditorView editor = Assert.IsType<RuleEditorView>(harness.Window.ActivePaneViews[PaneSlot.Editor]);
        editor.MoveTo(1);

        harness.Press(InspectCommands.InspectKey);

        Assert.Empty(harness.Window.InspectText);
        Assert.Contains("Nothing to inspect here", harness.Workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>A long text wraps to the width of the box, one word at a time.</summary>
    [Fact]
    public void Wrap_LongText_BreaksAtWordsWithinTheWidth_Test()
    {
        IReadOnlyList<string> lines = InspectPanel.Wrap("one two three four five six", 12);

        Assert.All(lines, line => Assert.True(line.Length <= 12, line));
        Assert.Equal("one two three four five six", string.Join(' ', lines));
    }

    private static string Describe(string rule, string path, Action<Workbench>? arrange = null)
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new(rule, [new RuleTextPane(), new TreePane(services.Services)]);
        arrange?.Invoke(harness.Workbench);
        return InspectController.For(harness.Workbench).DescribeNode(path) ?? throw new InvalidOperationException(path);
    }

    private static string DescribeAt(string rule, string marker, Action<Workbench>? arrange = null)
    {
        using WorkbenchHarness harness = new(rule, [new RuleTextPane()]);
        arrange?.Invoke(harness.Workbench);
        return InspectController.For(harness.Workbench).DescribeOffset(rule.IndexOf(marker, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(marker);
    }
}
