namespace TruthWeaver.Tool.Tui.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Analysis;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Tests.TestSupport;

/// <summary>The What would make this True panel: its lines, applying a pick, the work limit and the background search.</summary>
public sealed class CounterfactualPaneTests
{
    /// <summary>The panel lists the same change sets as the library call for the same rule and values.</summary>
    [Fact]
    public async Task RefreshAsync_RuleAndValues_ListsTheChangeSetsOfTheLibrary_Test()
    {
        Workbench workbench = Create("(b AND c) OR a");
        CounterfactualController controller = CounterfactualController.For(workbench);

        await controller.RefreshAsync();

        CounterfactualResult expected = RuleCounterfactual.FindChanges(workbench.Document.Rule!, workbench.Outcome.Decision!);
        string[] expectedLines = [.. expected.ChangeSets.Select(CounterfactualController.Describe)];
        Assert.Equal(expectedLines, controller.Lines.Where(l => l.Set is not null).Select(l => l.Text));
        Assert.Equal(2, expectedLines.Length);
    }

    /// <summary>Picking an entry sets the assigned values and the decision follows.</summary>
    [Fact]
    public async Task Apply_AChangeSet_SetsTheValuesAndTheDecisionBecomesTrue_Test()
    {
        Workbench workbench = Create("(b AND c) OR a");
        CounterfactualController controller = CounterfactualController.For(workbench);
        await controller.RefreshAsync();
        ChangeSet pick = controller.Lines.Last(l => l.Set is not null).Set!;

        bool applied = controller.Apply(pick);

        Assert.True(applied);
        Assert.Equal(TruthValue.True, workbench.Outcome.Decision!.Result);
        Assert.Equal(TruthValue.True, workbench.Assignment.ValueOf("b"));
        Assert.Equal(TruthValue.True, workbench.Assignment.ValueOf("c"));
    }

    /// <summary>When the decision already has the target value, the panel says so.</summary>
    [Fact]
    public async Task RefreshAsync_DecisionAlreadyTrue_SaysNoChangeIsNeeded_Test()
    {
        Workbench workbench = Create("a");
        workbench.Assignment.Set("a", TruthValue.True);
        workbench.NotifyAssignmentChanged();
        CounterfactualController controller = CounterfactualController.For(workbench);

        await controller.RefreshAsync();

        Assert.Contains(controller.Lines, l => l.Text.Contains("No change is needed", StringComparison.Ordinal));
    }

    /// <summary>A rule with more terms than the search decides shows the reason, not a hang.</summary>
    [Fact]
    public async Task RefreshAsync_RuleOverTheTermLimit_ShowsAMessage_Test()
    {
        string text = string.Join(" AND ", Enumerable.Range(0, 25).Select(i => $"t{i}"));
        Workbench workbench = Create(text);
        CounterfactualController controller = CounterfactualController.For(workbench);

        await controller.RefreshAsync();

        Assert.Contains(controller.Lines, l => l.Text.StartsWith("Not searched:", StringComparison.Ordinal));
        Assert.False(controller.IsSearching);
    }

    /// <summary>A search that stops at its cap shows a message that more sets exist.</summary>
    [Fact]
    public async Task RefreshAsync_MoreSetsThanTheCap_SaysTheListIsIncomplete_Test()
    {
        string text = string.Join(" OR ", Enumerable.Range(0, 12).Select(i => $"t{i}"));
        Workbench workbench = Create(text);
        CounterfactualController controller = CounterfactualController.For(workbench);

        await controller.RefreshAsync();

        Assert.Equal(RuleCounterfactual.DefaultMaxChangeSets, controller.Lines.Count(l => l.Set is not null));
        Assert.Contains(controller.Lines, l => l.Text.Contains("more exist", StringComparison.Ordinal));
    }

    /// <summary>The search runs on a background task: the lines say searching until the result is posted back.</summary>
    [Fact]
    public async Task RefreshAsync_WithAPostQueue_ReturnsBeforeTheResultIsApplied_Test()
    {
        Workbench workbench = Create("a OR b");
        Action? posted = null;
        TaskCompletionSource arrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        workbench.Post = action =>
        {
            posted = action;
            arrived.SetResult();
        };
        CounterfactualController controller = CounterfactualController.For(workbench);

        Task done = controller.RefreshAsync();

        Assert.True(controller.IsSearching);
        Assert.Contains(controller.Lines, l => l.Text == "Searching...");
        await arrived.Task.WaitAsync(TimeSpan.FromSeconds(20), TestContext.Current.CancellationToken);
        posted!();
        await done;
        Assert.False(controller.IsSearching);
        Assert.Contains(controller.Lines, l => l.Set is not null);
    }

    /// <summary>The target moves True, False, Unknown, and the header names it.</summary>
    [Fact]
    public async Task CycleTargetAsync_FromTrue_AimsForFalseAndSearchesAgain_Test()
    {
        Workbench workbench = Create("a");
        workbench.Assignment.Set("a", TruthValue.True);
        workbench.NotifyAssignmentChanged();
        CounterfactualController controller = CounterfactualController.For(workbench);

        await controller.CycleTargetAsync();

        Assert.Equal(TruthValue.False, controller.Target);
        Assert.Contains("make the rule False", controller.Lines[0].Text, StringComparison.Ordinal);
        Assert.Contains(controller.Lines, l => l.Set is not null);
    }

    /// <summary>The workbench registers the panel in the bottom region.</summary>
    [Fact]
    public void Default_Panes_IncludeTheCounterfactualPanel_Test()
    {
        Assert.Contains(WorkbenchPanes.Default, p => p is CounterfactualPane { Slot: PaneSlot.Bottom });
    }

    private static Workbench Create(string text)
    {
        return new Workbench(RuleDocument.Create(text), ToolSettings.Default, new ScriptedPrompts());
    }
}
