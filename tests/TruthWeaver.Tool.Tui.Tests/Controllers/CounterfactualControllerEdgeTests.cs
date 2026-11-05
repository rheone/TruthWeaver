namespace TruthWeaver.Tool.Tui.Tests.Controllers;

using TruthWeaver.Abstractions;
using TruthWeaver.Analysis;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The edge cases of the What would make this True panel: no decision, no possible change, stale results and a data reference.</summary>
public sealed class CounterfactualControllerEdgeTests
{
    /// <summary>A rule that does not compile has no decision, so the panel shows the reason and does not search.</summary>
    [Fact]
    public async Task RefreshAsync_RuleThatDoesNotCompile_ShowsTheReasonAndDoesNotSearch_Test()
    {
        Workbench workbench = Create("a AND");
        CounterfactualController controller = CounterfactualController.For(workbench);

        await controller.RefreshAsync();

        Assert.False(controller.IsSearching);
        Assert.Equal(2, controller.Lines.Count);
        Assert.Equal(workbench.Outcome.Reason, controller.Lines[1].Text);
    }

    /// <summary>The synchronous refresh does the same as the awaited one when there is no decision.</summary>
    [Fact]
    public void Refresh_RuleThatDoesNotCompile_ShowsTheReason_Test()
    {
        Workbench workbench = Create("a AND");
        CounterfactualController controller = CounterfactualController.For(workbench);

        controller.Refresh();

        Assert.Equal(workbench.Outcome.Reason, controller.Lines[1].Text);
    }

    /// <summary>The target cycles True, False, Unknown and back to True.</summary>
    [Fact]
    public void CycleTarget_ThreeTimes_VisitsFalseUnknownAndTrue_Test()
    {
        Workbench workbench = Create("a AND");
        CounterfactualController controller = CounterfactualController.For(workbench);

        List<TruthValue> targets = [];
        for (int i = 0; i < 3; i++)
        {
            controller.CycleTarget();
            targets.Add(controller.Target);
        }

        Assert.Equal([TruthValue.False, TruthValue.Unknown, TruthValue.True], targets);
    }

    /// <summary>A rule that no change of its terms can make True says so.</summary>
    [Fact]
    public async Task RefreshAsync_RuleThatCannotBeTrue_SaysNoChangeGivesTheTarget_Test()
    {
        Workbench workbench = Create("a AND NOT a");
        CounterfactualController controller = CounterfactualController.For(workbench);

        await controller.RefreshAsync();

        Assert.Contains(controller.Lines, l => l.Text == "No change of the terms gives True.");
    }

    /// <summary>A search result that arrives after a newer search started is dropped, so the older one cannot overwrite the newer.</summary>
    [Fact]
    public async Task RefreshAsync_ResultOfAnOlderSearch_IsIgnored_Test()
    {
        Workbench workbench = Create("a OR b");
        List<Action> posted = [];
        object gate = new();
        workbench.Post = action =>
        {
            lock (gate)
            {
                posted.Add(action);
            }
        };
        int PostedCount()
        {
            lock (gate)
            {
                return posted.Count;
            }
        }

        CounterfactualController controller = CounterfactualController.For(workbench);
        Task first = controller.RefreshAsync();
        await WaitForAsync(() => PostedCount() == 1);
        Task second = controller.RefreshAsync();
        await WaitForAsync(() => PostedCount() == 2);

        posted[0]();

        Assert.True(controller.IsSearching);
        posted[1]();
        await Task.WhenAll(first, second);
        Assert.False(controller.IsSearching);
    }

    /// <summary>A change to a term that takes its value from a data reference cannot be applied, and the status says why.</summary>
    [Fact]
    public void Apply_TermFromADataReference_IsNotAppliedAndSaysWhy_Test()
    {
        Workbench workbench = Create("inRegion(region: from(\"user\", \"$.region\"))");
        string term = workbench.Outcome.Terms[0].Display;
        CounterfactualController controller = CounterfactualController.For(workbench);
        ChangeSet set = new(new EquatableArray<TermChange>([new TermChange(term, TruthValue.Unknown, TruthValue.True)]));

        bool applied = controller.Apply(set);

        Assert.False(applied);
        Assert.StartsWith("Not applied:", workbench.Status, StringComparison.Ordinal);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(20);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "The searches did not post their results.");
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }

    private static Workbench Create(string text)
    {
        return new Workbench(RuleDocument.Create(text), ToolSettings.Default, new ScriptedPrompts());
    }
}
