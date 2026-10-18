namespace TruthWeaver.Tool.Tui.Tests;

using Terminal.Gui.Input;
using TruthWeaver.Tool.Tui.Tests.TestSupport;

/// <summary>Smoke tests of the Tree pane inside the workbench window.</summary>
[Collection(TerminalCollection.Name)]
public sealed class TreePaneTests
{
    /// <summary>Moving down the list selects a node, and the wrap key edits it and updates the editor text.</summary>
    [Fact]
    public void WrapKey_OnASelectedNode_EditsTheRuleText_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new TreePane(services.Services)]);
        harness.Window.FocusRegion(PaneSlot.Side);

        harness.Press(Key.CursorDown, new Key('w'));

        Assert.Equal("NOT a AND b", harness.Workbench.Document.Text);
        TreePaneView view = Assert.IsType<TreePaneView>(harness.Window.ActivePaneViews[PaneSlot.Side]);
        Assert.Equal(["and", "  not", "    a", "  b"], view.Lines);
        Assert.StartsWith("$.operands[0]", view.HeaderText, StringComparison.Ordinal);
    }

    /// <summary>The undo key of the window undoes a structural edit made in the pane.</summary>
    [Fact]
    public void UndoKey_AfterADelete_RestoresTheRule_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b AND c", [new RuleTextPane(), new TreePane(services.Services)]);
        harness.Window.FocusRegion(PaneSlot.Side);

        harness.Press(Key.CursorDown, Key.Delete);
        string deleted = harness.Workbench.Document.Text;
        harness.Press(Key.Z.WithCtrl);

        Assert.Equal("b AND c", deleted);
        Assert.Equal("a AND b AND c", harness.Workbench.Document.Text);
    }
}
