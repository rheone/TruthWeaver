namespace TruthWeaver.Tool.Tui.Tests.Controllers;

using Terminal.Gui.Input;
using TruthWeaver.Abstractions;
using TruthWeaver.Tool.Tui.Commands;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Panes;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Views;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>Tests of the step mode: stepping through an evaluation in the Tree pane.</summary>
[Collection(TerminalCollection.Name)]
public sealed class EvaluationStepControllerTests
{
    /// <summary>
    /// With <c>a</c> False, <c>AND</c> short-circuits: the steps are a, then b as skipped, then the operator, in
    /// evaluation order. Each step selects its node and the tree shows the value as a word.
    /// </summary>
    [Fact]
    public void Next_AndThatShortCircuits_StepsThroughTheNodesAndMarksTheSkippedOne_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new TreePane(services.Services)]);
        harness.Workbench.Assignment.Set("a", TruthValue.False);
        harness.Workbench.NotifyAssignmentChanged();
        EvaluationStepController steps = EvaluationStepController.For(harness.Workbench);
        TreePaneView view = Assert.IsType<TreePaneView>(harness.Window.ActivePaneViews[PaneSlot.Side]);

        harness.Press(StepCommands.NextKey);
        Assert.Equal("$.operands[0]", harness.Workbench.SelectedNode);
        Assert.Contains("Step 1 of 3: a = False", harness.Workbench.Status, StringComparison.Ordinal);
        Assert.EndsWith("  => False", view.Lines[1], StringComparison.Ordinal);
        Assert.DoesNotContain("=>", view.Lines[2], StringComparison.Ordinal);

        harness.Press(StepCommands.NextKey);
        Assert.Equal("$.operands[1]", harness.Workbench.SelectedNode);
        Assert.Contains("Step 2 of 3: b = skipped", harness.Workbench.Status, StringComparison.Ordinal);
        Assert.EndsWith("  => skipped", view.Lines[2], StringComparison.Ordinal);

        harness.Press(StepCommands.NextKey);
        Assert.Equal("$", harness.Workbench.SelectedNode);
        Assert.EndsWith("  => False", view.Lines[0], StringComparison.Ordinal);

        harness.Press(StepCommands.PreviousKey);
        Assert.Equal(1, steps.Index);
        Assert.DoesNotContain("=>", view.Lines[0], StringComparison.Ordinal);

        harness.Press(Key.Esc);
        Assert.False(steps.IsActive);
        Assert.DoesNotContain("=>", string.Concat(view.Lines), StringComparison.Ordinal);
    }

    /// <summary>A change of a value ends the step mode, because the steps would describe the old values.</summary>
    [Fact]
    public void Next_AfterTheAssignmentChanges_LeavesTheStepMode_Test()
    {
        ScriptedServices services = new();
        using WorkbenchHarness harness = new("a AND b", [new RuleTextPane(), new TreePane(services.Services)]);
        EvaluationStepController steps = EvaluationStepController.For(harness.Workbench);

        harness.Press(StepCommands.NextKey);
        harness.Workbench.Assignment.Set("a", TruthValue.True);
        harness.Workbench.NotifyAssignmentChanged();

        Assert.False(steps.IsActive);
    }
}
