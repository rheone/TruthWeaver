namespace TruthWeaver.Tool.Tui.Tests.Panes;

using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using TruthWeaver.Abstractions;
using TruthWeaver.Printing;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>Smoke tests of the view pane inside the workbench window.</summary>
[Collection(TerminalCollection.Name)]
public sealed class ViewPaneTests
{
    /// <summary>The pane shows the tree first, and the view key switches the form with the text following the document.</summary>
    [Fact]
    public void ViewKey_InTheViewPane_SwitchesTheFormAndFollowsTheDocument_Test()
    {
        ScriptedServices services = new();
        ViewPane pane = new(services.Services);
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), pane]);
        harness.Window.FocusRegion(PaneSlot.Side);
        ViewPaneView view = View(harness);
        Assert.Contains("AND", view.BodyText, StringComparison.Ordinal);

        harness.Press(new Key('v'), new Key('v'));
        harness.Workbench.Document.SetText("a OR b");

        Assert.Equal(ViewKind.Equation, pane.Controller!.Settings.Kind);
        Assert.Equal(pane.Controller.Output.Display, view.BodyText);
        Assert.Contains("a", view.BodyText, StringComparison.Ordinal);
    }

    /// <summary>The copy key puts the shown text on the clipboard.</summary>
    [Fact]
    public void CopyKey_InTheViewPane_PutsTheTextOnTheClipboard_Test()
    {
        ScriptedServices services = new();
        ViewPane pane = new(services.Services);
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), pane]);
        harness.Window.FocusRegion(PaneSlot.Side);

        harness.Press(new Key('y'));

        Assert.Equal(pane.Controller!.Output.Text, services.Clipboard.Text);
    }

    /// <summary>
    /// With no evaluation source of its own, the pane marks the decision of the assignment, and a change of a value shows
    /// in the tree at once. The text view gives its lines back with the line ending of the platform, so the test compares LF text.
    /// </summary>
    [Fact]
    public void CreateView_AssignmentChanged_MarksTheDecisionOfTheAssignment_Test()
    {
        ScriptedServices services = new();
        ViewPane pane = new(services.Services);
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), pane]);
        ViewPaneView view = View(harness);

        harness.Workbench.Assignment.Set("a", TruthValue.True);
        harness.Workbench.Assignment.Set("b", TruthValue.True);
        harness.Workbench.NotifyAssignmentChanged();

        StubEvaluation evaluation = harness.Workbench.Outcome.Evaluation!;
        Assert.Contains("decision: shown", pane.Controller!.Header, StringComparison.Ordinal);
        Assert.Equal(TruthValue.True, evaluation.Decision.Result);
        Assert.Equal(evaluation.Rule.PrintPlainText(evaluation.Decision).ReplaceLineEndings("\n"), pane.Controller.Output.Text);
        Assert.Equal(pane.Controller.Output.Display, view.BodyText.ReplaceLineEndings("\n"));
    }

    /// <summary>The workbench registers the view pane in the side region after the canonical pane.</summary>
    [Fact]
    public void Default_Panes_IncludeTheViewPaneInTheSideRegion_Test()
    {
        Assert.Contains(WorkbenchPanes.Default, p => p is ViewPane { Slot: PaneSlot.Side });
    }

    private static ViewPaneView View(WorkbenchHarness harness)
    {
        return Assert.IsType<ViewPaneView>(harness.Window.ActivePaneViews[PaneSlot.Side]);
    }
}
