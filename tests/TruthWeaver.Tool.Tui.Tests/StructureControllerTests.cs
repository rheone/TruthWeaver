namespace TruthWeaver.Tool.Tui.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Tool.Core;
using TruthWeaver.Tool.Tui.Tests.TestSupport;

/// <summary>The structural editor of the workbench with no terminal: edits, the palette, the clipboard, undo and redo.</summary>
public sealed class StructureControllerTests
{
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
        services.Dialogs.Choices.Enqueue(1 + entries.FindIndex(e => e.Name == "hasRole"));
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

    private static (StructureController Controller, ScriptedServices Services, Workbench Workbench) Create(string text)
    {
        ScriptedServices services = new();
        Workbench workbench = new(RuleDocument.Create(text), ToolSettings.Default, new ScriptedPrompts());
        return (new StructureController(workbench, services.Services), services, workbench);
    }
}
