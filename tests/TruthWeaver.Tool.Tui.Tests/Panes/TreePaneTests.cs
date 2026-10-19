namespace TruthWeaver.Tool.Tui.Tests.Panes;

using Terminal.Gui.Input;
using TruthWeaver.Abstractions;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

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
        Assert.Equal(["and", "  not", "    a  [Unknown]", "  b  [Unknown]"], view.Lines);
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

    /// <summary>A term with no value in the assignment shows the word Unknown, in the tree and on the status line.</summary>
    [Fact]
    public void Show_TermWithNoAssignment_ShowsUnknownInTheTreeAndTheStatusLine_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new TreePane(services.Services)]);
        harness.Window.FocusRegion(PaneSlot.Side);

        harness.Press(Key.CursorDown);

        TreePaneView view = Assert.IsType<TreePaneView>(harness.Window.ActivePaneViews[PaneSlot.Side]);
        Assert.Equal(["and", "  a  [Unknown]", "  b  [Unknown]"], view.Lines);
        Assert.Contains("a = Unknown", WorkbenchWindow.StatusOf(harness.Workbench), StringComparison.Ordinal);
    }

    /// <summary>A term takes the value of the current assignment, and the tree and the status line follow a change.</summary>
    [Fact]
    public void Show_AfterTheAssignmentChanges_UpdatesTheBadgeAndTheStatusLine_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new TreePane(services.Services)]);
        harness.Window.FocusRegion(PaneSlot.Side);
        harness.Press(Key.CursorDown);

        harness.Workbench.Assignment.Set("a", TruthValue.True);
        harness.Workbench.NotifyAssignmentChanged();

        TreePaneView view = Assert.IsType<TreePaneView>(harness.Window.ActivePaneViews[PaneSlot.Side]);
        Assert.Equal("  a  [True]", view.Lines[1]);
        Assert.Contains("a = True", WorkbenchWindow.StatusOf(harness.Workbench), StringComparison.Ordinal);

        harness.Workbench.Assignment.Set("a", TruthValue.False);
        harness.Workbench.NotifyAssignmentChanged();

        Assert.Equal("  a  [False]", view.Lines[1]);
        Assert.Contains("a = False", WorkbenchWindow.StatusOf(harness.Workbench), StringComparison.Ordinal);
    }

    /// <summary>A node that is not a term, such as an operator, adds no value to the status line.</summary>
    [Fact]
    public void StatusOf_OperatorSelected_ShowsNoValue_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new TreePane(services.Services)]);

        string status = WorkbenchWindow.StatusOf(harness.Workbench);

        Assert.DoesNotContain(" = ", status, StringComparison.Ordinal);
    }

    /// <summary>A term with a from(...) source shows the resolved value, or the reason it has none.</summary>
    [Fact]
    public void Show_TermWithAFromSource_ShowsTheResolvedValueOrTheFailure_Test()
    {
        const string Term = "inRegion(region: from(\"user\", \"$.region\"))";
        ScriptedServices services = new();
        using WorkbenchHarness harness = new(Term, [new RuleTextPane(), new TreePane(services.Services)]);
        TreePaneView view = Assert.IsType<TreePaneView>(harness.Window.ActivePaneViews[PaneSlot.Side]);

        string withoutData = view.Lines[0];

        Assert.StartsWith(Term + "  [Unknown: ", withoutData, StringComparison.Ordinal);
        Assert.Contains("= Unknown: ", WorkbenchWindow.StatusOf(harness.Workbench), StringComparison.Ordinal);

        _ = harness.Workbench.Assignment.SetVariables("{\"user\": {\"region\": \"EU\"}}");
        harness.Workbench.Assignment.Set("inRegion(region: \"EU\")", TruthValue.True);
        harness.Workbench.NotifyAssignmentChanged();

        Assert.Equal(Term + "  [True]", view.Lines[0]);
        Assert.Contains("= True", WorkbenchWindow.StatusOf(harness.Workbench), StringComparison.Ordinal);
    }
}
