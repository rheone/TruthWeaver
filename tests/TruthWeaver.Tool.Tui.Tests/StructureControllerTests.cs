namespace TruthWeaver.Tool.Tui.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Tool.Core;
using TruthWeaver.Tool.Tui.Tests.TestSupport;

/// <summary>The structural editor of the workbench with no terminal: edits, the palette, the clipboard, undo and redo.</summary>
public sealed class StructureControllerTests
{
    private const string DefinitionsText = "let both(x, y) = x AND y;\nboth(a, b) OR c";

    /// <summary>An edit changes the editor text and the decision at once.</summary>
    [Fact]
    public void WrapInNot_SelectedOperand_ChangesTheTextAndTheDecision_Test()
    {
        (StructureController controller, _, Workbench workbench) = Create("a AND b");
        workbench.Assignment.Set("a", TruthValue.True);
        workbench.Assignment.Set("b", TruthValue.False);
        workbench.NotifyAssignmentChanged();
        Assert.Equal(TruthValue.False, workbench.Outcome.Decision!.Result);
        workbench.SelectNode("$.operands[1]");

        bool changed = controller.WrapInNot();

        Assert.True(changed);
        Assert.Equal("a AND NOT b", workbench.Document.Text);
        Assert.Equal(TruthValue.True, workbench.Outcome.Decision.Result);
        Assert.Equal("$.operands[1]", workbench.SelectedNode);
    }

    /// <summary>An edit that would make an invalid rule is refused with a message and changes nothing.</summary>
    [Fact]
    public void ReplaceOperator_NotOfTwoOperands_IsRefusedWithAMessage_Test()
    {
        (StructureController controller, ScriptedServices services, Workbench workbench) = Create("a AND b");
        int not = controller
            .Palette()
            .Entries.Where(e => e.Source == PaletteSource.Operator)
            .ToList()
            .FindIndex(e => e.Name == "not");
        services.Dialogs.Choices.Enqueue(not);

        bool changed = controller.ReplaceOperator();

        Assert.False(changed);
        Assert.Equal("a AND b", workbench.Document.Text);
        Assert.False(workbench.Document.CanUndo);
        Assert.StartsWith("Not applied: the edit would make an invalid rule.", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>Undo and redo of an edit restore the text and the tree exactly.</summary>
    [Fact]
    public void Undo_AfterAnEdit_RestoresTheTextAndTheTreeExactly_Test()
    {
        (StructureController controller, _, Workbench workbench) = Create("a AND (b OR c)");
        string text = workbench.Document.Text;
        string tree = workbench.Document.Rule!.PrintJson();
        workbench.SelectNode("$.operands[1].operands[0]");
        controller.Delete();
        (string Text, string Tree) edited = (workbench.Document.Text, workbench.Document.Rule.PrintJson());

        workbench.Undo();
        (string Text, string Tree) undone = (workbench.Document.Text, workbench.Document.Rule.PrintJson());
        workbench.Redo();

        Assert.Equal("a AND c", edited.Text);
        Assert.Equal((text, tree), undone);
        Assert.Equal(edited, (workbench.Document.Text, workbench.Document.Rule.PrintJson()));
    }

    /// <summary>Pasting text that is not a valid expression is rejected with its diagnostic, and the rule stays.</summary>
    [Fact]
    public void Paste_InvalidText_IsRejectedWithTheDiagnostics_Test()
    {
        (StructureController controller, ScriptedServices services, Workbench workbench) = Create("a AND b");
        services.Clipboard.Text = "c AND (";
        workbench.SelectNode("$.operands[1]");

        bool changed = controller.Paste();

        Assert.False(changed);
        Assert.Equal("a AND b", workbench.Document.Text);
        Assert.StartsWith("Not a valid expression: TRE", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>Pasting a valid expression puts it in place of the selected node.</summary>
    [Fact]
    public void Paste_ValidExpression_ReplacesTheSelectedNode_Test()
    {
        (StructureController controller, ScriptedServices services, Workbench workbench) = Create("a AND b");
        services.Clipboard.Text = "c OR d";
        workbench.SelectNode("$.operands[1]");

        bool changed = controller.Paste();

        Assert.True(changed);
        Assert.Equal("a AND (c OR d)", workbench.Document.Text);
    }

    /// <summary>Copy puts the selected node on the clipboard as rule text, and the whole rule on request.</summary>
    [Fact]
    public void CopyNode_SelectedOperand_PutsItsRuleTextOnTheClipboard_Test()
    {
        (StructureController controller, ScriptedServices services, Workbench workbench) = Create("x AND (a OR b)");
        workbench.SelectNode("$.operands[1]");

        controller.CopyNode();
        string? node = services.Clipboard.Text;
        controller.CopyRule();

        Assert.Equal("a OR b", node);
        Assert.Equal("x AND (a OR b)", services.Clipboard.Text);
    }

    /// <summary>Inserting a typed expression adds it as the last operand and selects it.</summary>
    [Fact]
    public void InsertOperand_TypedExpression_AddsItAndSelectsIt_Test()
    {
        (StructureController controller, ScriptedServices services, Workbench workbench) = Create("a AND b");
        services.Dialogs.Choices.Enqueue(0);
        services.Dialogs.Texts.Enqueue("c");

        bool changed = controller.InsertOperand();

        Assert.True(changed);
        Assert.Equal("a AND b AND c", workbench.Document.Text);
        Assert.Equal("$.operands[2]", workbench.SelectedNode);
    }

    /// <summary>A predicate of the palette asks for its arguments and replaces the selected node with the call.</summary>
    [Fact]
    public void ReplaceFromPalette_PredicateOfTheRule_FillsTheFormAndReplacesTheNode_Test()
    {
        (StructureController controller, ScriptedServices services, Workbench workbench) = Create(
            "a AND hasRole(role: \"viewer\")"
        );
        List<PaletteEntry> entries = [.. controller.Palette().Entries.Where(e => e.Source != PaletteSource.Operator)];
        services.Dialogs.Choices.Enqueue(2 + entries.FindIndex(e => e.Name == "hasRole"));
        services.Dialogs.Texts.Enqueue("admin");
        workbench.SelectNode("$.operands[0]");

        bool changed = controller.ReplaceFromPalette();

        Assert.True(changed);
        Assert.Equal("hasRole(role: \"admin\") AND hasRole(role: \"viewer\")", workbench.Document.Text);
    }

    /// <summary>Changing a literal asks for the value, starting from the current one.</summary>
    [Fact]
    public void ChangeLiteral_ConstantOperand_ChangesTheConstant_Test()
    {
        (StructureController controller, ScriptedServices services, Workbench workbench) = Create("a OR False");
        services.Dialogs.Texts.Enqueue("true");
        workbench.SelectNode("$.operands[1]");

        bool changed = controller.ChangeLiteral();

        Assert.True(changed);
        Assert.Equal("a OR True", workbench.Document.Text);
    }

    /// <summary>While the text has errors, an edit of the last valid tree is refused, so the typed text is not lost.</summary>
    [Fact]
    public void Negate_TextWithErrors_IsRefused_Test()
    {
        (StructureController controller, _, Workbench workbench) = Create("a AND b");
        workbench.Document.SetText("a AND (");

        bool changed = controller.Negate();

        Assert.False(changed);
        Assert.Equal("a AND (", workbench.Document.Text);
        Assert.StartsWith("Cannot edit the tree: the rule text has errors", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>The lines show each node indented by depth, and a node that the new tree lacks is no longer selected.</summary>
    [Fact]
    public void Lines_AfterAnEditThatRemovesTheSelectedNode_SelectTheRoot_Test()
    {
        (StructureController controller, _, Workbench workbench) = Create("a AND (b OR c)");
        workbench.SelectNode("$.operands[1].operands[1]");

        workbench.Document.SetText("a AND b");

        Assert.Equal(["and", "  a", "  b"], controller.Lines);
        Assert.Equal("$", workbench.SelectedNode);
    }

    /// <summary>
    /// With <c>let</c> definitions, the lines mark the node that a use expands to, show the body below it with the
    /// arguments in place, and list the definition with its parameters after the rule.
    /// </summary>
    [Fact]
    public void Lines_RuleWithDefinitions_ShowTheUseAndTheDefinition_Test()
    {
        (StructureController controller, _, _) = Create(DefinitionsText);

        Assert.Equal(
            [
                "or",
                "  use both(a, b) = and",
                "    a",
                "    b",
                "  c",
                "let both(x, y)",
                "  and",
                "    x (parameter)",
                "    y (parameter)",
            ],
            controller.Lines
        );
    }

    /// <summary>An edit of the root of a use changes the definition body, and the text keeps the definition and the use.</summary>
    [Fact]
    public void ReplaceOperator_RootOfAUse_EditsTheDefinitionAndKeepsIt_Test()
    {
        (StructureController controller, ScriptedServices services, Workbench workbench) = Create(DefinitionsText);
        int or = controller
            .Palette()
            .Entries.Where(e => e.Source == PaletteSource.Operator)
            .ToList()
            .FindIndex(e => e.Name == "or");
        services.Dialogs.Choices.Enqueue(or);
        workbench.SelectNode("$.operands[0]");

        bool changed = controller.ReplaceOperator();

        Assert.True(changed);
        Assert.Equal("let both(x, y) = x OR y;\nboth(a, b) OR c", workbench.Document.Text);
        Assert.Equal("$.operands[0]", workbench.SelectedNode);
    }

    /// <summary>A definition of the palette asks for its arguments and puts a use of the definition in place of the node.</summary>
    [Fact]
    public void ReplaceFromPalette_Definition_PutsAUseIn_Test()
    {
        (StructureController controller, ScriptedServices services, Workbench workbench) = Create(DefinitionsText);
        List<PaletteEntry> entries = [.. controller.Palette().Entries.Where(e => e.Source != PaletteSource.Operator)];
        services.Dialogs.Choices.Enqueue(entries.FindIndex(e => e.Source == PaletteSource.Definition) + 2);
        services.Dialogs.Texts.Enqueue("c");
        services.Dialogs.Texts.Enqueue("d");
        workbench.SelectNode("$.operands[1]");

        bool changed = controller.ReplaceFromPalette();

        Assert.True(changed);
        Assert.Equal("let both(x, y) = x AND y;\nboth(a, b) OR both(c, d)", workbench.Document.Text);
    }

    /// <summary>An edit that would break the definition is refused with a message and changes nothing.</summary>
    [Fact]
    public void ChangeLiteral_ParameterOfADefinition_IsRefusedWithAMessage_Test()
    {
        (StructureController controller, ScriptedServices services, Workbench workbench) = Create(
            "let one = both(a, b); let both(x, y) = x AND isAdmin(level: 1); one"
        );
        services.Dialogs.Texts.Enqueue("2");
        workbench.SelectNode("$.definitions.both.body.operands[1]");
        Assert.True(controller.ChangeLiteral());
        workbench.SelectNode("$.definitions.both.body.operands[0]");

        bool changed = controller.ChangeLiteral();

        Assert.False(changed);
        Assert.Equal("The selected node has no literal to change.", workbench.Status);
        Assert.Contains("isAdmin(level: 2)", workbench.Document.Text, StringComparison.Ordinal);
        Assert.StartsWith("let one = both(a, b);", workbench.Document.Text, StringComparison.Ordinal);
    }

    private static (StructureController Controller, ScriptedServices Services, Workbench Workbench) Create(string text)
    {
        ScriptedServices services = new();
        Workbench workbench = new(RuleDocument.Create(text), ToolSettings.Default, new ScriptedPrompts());
        return (new StructureController(workbench, services.Services), services, workbench);
    }
}
