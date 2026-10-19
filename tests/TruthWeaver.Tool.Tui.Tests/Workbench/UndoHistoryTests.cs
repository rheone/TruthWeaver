namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using Terminal.Gui.Input;
using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Rewriting;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The one undo and redo history of the workbench: editor typing, tree edits, rewrites, conversions and samples share it.</summary>
public sealed class UndoHistoryTests
{
    /// <summary>The Edit menu lists Undo and Redo with Ctrl+Z and Ctrl+Y.</summary>
    [Fact]
    public void Default_EditGroup_ListsUndoAndRedoWithTheirKeys_Test()
    {
        WorkbenchCommand[] edit = [.. WorkbenchCommands.Default.Where(c => c.Group == CommandGroup.Edit)];

        Assert.Equal(Key.Z.WithCtrl, edit.Single(c => c.Title == "Undo").Key);
        Assert.Equal(Key.Y.WithCtrl, edit.Single(c => c.Title == "Redo").Key);
    }

    /// <summary>Typing in the editor and a tree edit are steps of one history: Ctrl+Z undoes them newest first, Ctrl+Y does them again.</summary>
    [Fact]
    public void CtrlZ_AfterTypingAndATreeEdit_UndoesBothInOrder_Test()
    {
        using WorkbenchHarness harness = new("a AND b");
        harness.Press(Key.End.WithCtrl);
        harness.Type("c");
        StructureController tree = new(harness.Workbench, new ScriptedServices().Services);
        harness.Workbench.SelectNode("$.operands[1]");
        tree.Delete();
        string afterTree = harness.Workbench.Document.Text;

        harness.Press(Key.Z.WithCtrl);
        string afterFirstUndo = harness.Workbench.Document.Text;
        harness.Press(Key.Z.WithCtrl);
        string afterSecondUndo = harness.Workbench.Document.Text;
        harness.Press(Key.Y.WithCtrl, Key.Y.WithCtrl);

        Assert.NotEqual("a AND bc", afterTree);
        Assert.Equal("a AND bc", afterFirstUndo);
        Assert.Equal("a AND b", afterSecondUndo);
        Assert.Equal(afterTree, harness.Workbench.Document.Text);
        Assert.Equal(
            afterTree,
            Assert.IsType<RuleEditorView>(harness.Window.ActivePaneViews[PaneSlot.Editor]).Text.ReplaceLineEndings("\n")
        );
    }

    /// <summary>A conversion is one undo step: undo gives back the text and the format.</summary>
    [Fact]
    public void Undo_AfterAConversion_RestoresTheTextAndTheFormat_Test()
    {
        Workbench workbench = new(RuleDocument.Create("a AND b"), ToolSettings.Default, new ScriptedPrompts());

        ImportController.For(workbench).ConvertTo(RuleFormat.Json);
        RuleFormat converted = workbench.Document.Format;
        workbench.Undo();

        Assert.Equal(RuleFormat.Json, converted);
        Assert.Equal(RuleFormat.Text, workbench.Document.Format);
        Assert.Equal("a AND b", workbench.Document.Text);
        Assert.True(workbench.Document.CanRedo);
    }

    /// <summary>An accepted rewrite is one undo step.</summary>
    [Fact]
    public void Undo_AfterAnAcceptedRewrite_RestoresTheRule_Test()
    {
        Workbench workbench = new(RuleDocument.Create("NOT (a AND b)"), ToolSettings.Default, new ScriptedPrompts());
        RewriteController rewrite = new(workbench, new ScriptedServices().Services);
        rewrite.Run(RuleRewriteKind.NegationNormalForm);
        rewrite.Accept();

        workbench.Undo();

        Assert.Equal("NOT (a AND b)", workbench.Document.Text);
    }

    /// <summary>A sample is one undo step, and redo puts the sample back.</summary>
    [Fact]
    public void Undo_AfterASample_RestoresTheRuleAndRedoReturnsTheSample_Test()
    {
        Workbench workbench = new(RuleDocument.Create("a"), ToolSettings.Default, new ScriptedPrompts());
        SampleRule sample = SampleRules.All[0];
        workbench.OpenSample(sample);

        workbench.Undo();
        string undone = workbench.Document.Text;
        workbench.Redo();

        Assert.Equal("a", undone);
        Assert.Equal(sample.Text, workbench.Document.Text);
    }

    /// <summary>Text that waits for its compile is its own step before a replacement, so one undo gives back the typed text.</summary>
    [Fact]
    public void ReplaceText_AfterPendingTyping_KeepsTheTypedTextAsAnEarlierStep_Test()
    {
        Workbench workbench = new(RuleDocument.Create("a"), ToolSettings.Default, new ScriptedPrompts())
        {
            DiagnosticsDelay = TimeSpan.FromHours(1),
        };
        workbench.EditText("a AND b");

        workbench.ReplaceText("c", RuleFormat.Text, "Replaced.");
        workbench.Undo();

        Assert.Equal("a AND b", workbench.Document.Text);
    }
}
