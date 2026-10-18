namespace TruthWeaver.Tool.Tui.Tests.Workbench;

using System.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>Tests of the mouse in the workbench window: a click focuses a pane and selects a tree node, the wheel scrolls.</summary>
[Collection(TerminalCollection.Name)]
public sealed class MouseTests
{
    /// <summary>A click in the Tree pane gives the focus to its region.</summary>
    [Fact]
    public void Click_InAnotherRegion_FocusesThatRegion_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new TreePane(services.Services)]);
        TreePaneView tree = Assert.IsType<TreePaneView>(harness.Window.ActivePaneViews[PaneSlot.Side]);
        Assert.Equal(PaneSlot.Editor, harness.Window.FocusedSlot());

        harness.Click(tree.List, 1, 0);

        Assert.Equal(PaneSlot.Side, harness.Window.FocusedSlot());
    }

    /// <summary>A click on a line of the Tree pane selects that node, so the edit keys act on it.</summary>
    [Fact]
    public void Click_OnATreeNode_SelectsTheNode_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new TreePane(services.Services)]);
        TreePaneView tree = Assert.IsType<TreePaneView>(harness.Window.ActivePaneViews[PaneSlot.Side]);

        harness.Click(tree.List, 2, 2);

        Assert.StartsWith("$.operands[1]", tree.HeaderText, StringComparison.Ordinal);
        harness.Press(new Key('w'));
        Assert.Equal("a AND NOT b", harness.Workbench.Document.Text);
    }

    /// <summary>The wheel scrolls the list of the Tree pane without a key.</summary>
    [Fact]
    public void Wheel_OverALongTree_ScrollsTheList_Test()
    {
        ScriptedServices services = new();
        string rule = string.Join(" AND ", Enumerable.Range(0, 60).Select(i => $"t{i}"));
        using WorkbenchHarness harness = new(rule, [new RuleTextPane(), new TreePane(services.Services)]);
        TreePaneView tree = Assert.IsType<TreePaneView>(harness.Window.ActivePaneViews[PaneSlot.Side]);
        int before = tree.List.Viewport.Y;

        harness.Wheel(tree.List, down: true, steps: 3);

        Assert.True(tree.List.Viewport.Y > before);
    }

    /// <summary>The key help says that the mouse works and that every action keeps its key.</summary>
    [Fact]
    public void HelpText_DefaultCommands_SaysTheMouseWorks_Test()
    {
        string help = WorkbenchWindow.HelpText(WorkbenchCommands.Default);

        Assert.Contains("Mouse:", help, StringComparison.Ordinal);
        Assert.Contains("Every action has a key", help, StringComparison.Ordinal);
    }
}
