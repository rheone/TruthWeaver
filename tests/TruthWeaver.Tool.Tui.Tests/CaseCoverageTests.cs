namespace TruthWeaver.Tool.Tui.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Tui.Tests.TestSupport;

/// <summary>
/// The coverage of the open rule over the saved cases in the Tree pane: the marks on the nodes that no case reaches, the
/// summary line, and the update after a case is added, changed or removed.
/// </summary>
[Collection(TerminalCollection.Name)]
public sealed class CaseCoverageTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "truthweaver-tui-tests", Guid.NewGuid().ToString("N"));

    public CaseCoverageTests()
    {
        _ = Directory.CreateDirectory(this.directory);
    }

    /// <summary>A rule with no saved cases shows a message, not zero percent, and no node is marked.</summary>
    [Fact]
    public void Summary_NoSavedCases_ShowsAMessageAndNoMarks_Test()
    {
        using WorkbenchHarness harness = Create("a OR b");

        TreePaneView view = Show(harness);

        Assert.Equal("No saved cases: coverage is not measured.", view.CoverageText);
        Assert.Equal(["or", "  a", "  b"], view.Lines);
        Assert.DoesNotContain("%", view.CoverageText, StringComparison.Ordinal);
    }

    /// <summary>The marks and the summary equal the library coverage of the same rule over the same cases.</summary>
    [Fact]
    public void Marks_OneCaseThatShortCircuits_EqualTheLibraryCoverage_Test()
    {
        using WorkbenchHarness harness = Create("a OR b");
        this.SaveCase(harness, "a decides", ("a", TruthValue.True));

        TreePaneView view = Show(harness);

        RuleCoverage coverage = Assert.IsType<RuleCoverage>(harness.Workbench.Coverage.Coverage);
        string[] unreached = [.. coverage.Nodes.Where(n => n.EvaluatedCount == 0).Select(n => n.Path)];
        Assert.Equal(["$.operands[1]"], unreached);
        Assert.Equal(["or", "  a", "  b" + StructureController.NoCaseMark], view.Lines);
        Assert.Equal("Coverage: 2 of 3 node(s) reached by 1 case(s) (67%).", view.CoverageText);
    }

    /// <summary>Adding a case that reaches the skipped node removes its mark and raises the share.</summary>
    [Fact]
    public void Marks_AfterACaseIsAdded_Update_Test()
    {
        using WorkbenchHarness harness = Create("a OR b");
        this.SaveCase(harness, "a decides", ("a", TruthValue.True));
        TreePaneView view = Show(harness);
        string[] before = [.. view.Lines];

        this.SaveCase(harness, "b is read", ("a", TruthValue.False), ("b", TruthValue.True));

        Assert.Equal("  b" + StructureController.NoCaseMark, before[2]);
        Assert.Equal(["or", "  a", "  b"], view.Lines);
        Assert.Equal("Coverage: 3 of 3 node(s) reached by 2 case(s) (100%).", view.CoverageText);
    }

    /// <summary>Changing a case, by saving over its name, moves the marks.</summary>
    [Fact]
    public void Marks_AfterACaseIsChanged_Update_Test()
    {
        using WorkbenchHarness harness = Create("a OR b");
        this.SaveCase(harness, "case", ("a", TruthValue.False), ("b", TruthValue.True));
        TreePaneView view = Show(harness);
        string[] before = [.. view.Lines];

        this.SaveCase(harness, "case", ("a", TruthValue.True));

        Assert.Equal(["or", "  a", "  b"], before);
        Assert.Equal(["or", "  a", "  b" + StructureController.NoCaseMark], view.Lines);
    }

    /// <summary>Removing a case brings back the marks that only it covered.</summary>
    [Fact]
    public void Marks_AfterACaseIsRemoved_Update_Test()
    {
        using WorkbenchHarness harness = Create("a OR b");
        this.SaveCase(harness, "a decides", ("a", TruthValue.True));
        this.SaveCase(harness, "b is read", ("a", TruthValue.False), ("b", TruthValue.True));
        TreePaneView view = Show(harness);
        string[] before = [.. view.Lines];

        harness.Workbench.DeleteCase(1);

        Assert.Equal(["or", "  a", "  b"], before);
        Assert.Equal(["or", "  a", "  b" + StructureController.NoCaseMark], view.Lines);
    }

    /// <summary>A change of the rule is measured again over the same cases.</summary>
    [Fact]
    public void Marks_AfterTheRuleChanges_FollowTheNewRule_Test()
    {
        using WorkbenchHarness harness = Create("a OR b");
        this.SaveCase(harness, "a decides", ("a", TruthValue.True));
        TreePaneView view = Show(harness);

        harness.Workbench.Document.SetText("a AND b");
        view.Show();

        Assert.Equal(["and", "  a", "  b"], view.Lines);
        Assert.Equal("Coverage: 3 of 3 node(s) reached by 1 case(s) (100%).", view.CoverageText);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Directory.Delete(this.directory, recursive: true);
    }

    private static WorkbenchHarness Create(string text)
    {
        ScriptedServices services = new();
        return new WorkbenchHarness(text, [new RuleTextPane(), new TreePane(services.Services)]);
    }

    private static TreePaneView Show(WorkbenchHarness harness)
    {
        return Assert.IsType<TreePaneView>(harness.Window.ActivePaneViews[PaneSlot.Side]);
    }

    private void SaveCase(WorkbenchHarness harness, string name, params (string Term, TruthValue Value)[] values)
    {
        harness.Workbench.Assignment.ClearValues();
        foreach ((string term, TruthValue value) in values)
        {
            harness.Workbench.Assignment.Set(term, value);
        }

        harness.Workbench.RuleTests.FilePath = Path.Combine(this.directory, "cases.test.yaml");
        harness.Workbench.RuleTests.CaseName = name;
        harness.Workbench.SaveCase();
    }
}
