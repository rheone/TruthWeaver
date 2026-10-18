namespace TruthWeaver.Tool.Tui.Tests.Controllers;

using TruthWeaver.Abstractions;
using TruthWeaver.Analysis;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Tests.TestSupport;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>The second rule of the Compare pane, with no terminal.</summary>
public sealed class CompareControllerTests
{
    /// <summary>The relation and the overlap in the pane are the ones the library gives for the same two rules.</summary>
    [Fact]
    public void FromClipboard_NarrowerRule_ShowsTheRelationAndOverlapOfTheLibrary_Test()
    {
        (CompareController controller, ScriptedServices services, Workbench workbench) = Create("a OR b");
        services.Clipboard.Text = "a";
        CompiledRule<StubAssignment> first = workbench.Document.Rule!;
        CompiledRule<StubAssignment> second = RuleDocument.Create("a").Rule!;
        RuleRelationResult relation = RuleImplication.Compare(first, second, 20);
        RuleOverlapResult overlap = RuleOverlap.Compare(first, second, 20);

        bool loaded = controller.FromClipboard();

        Assert.True(loaded);
        Assert.Equal(RuleRelation.Tighter, relation.Relation);
        Assert.Contains(controller.Lines, l => l.Text.StartsWith("The second rule is tighter", StringComparison.Ordinal));
        Assert.Contains(
            controller.Lines,
            l => l.Text.Contains("Satisfies the open rule only", StringComparison.Ordinal) && l.Example is not null
        );
        foreach (JointOutcome pair in overlap.Reachable.Where(p => p.First != p.Second))
        {
            Assert.Contains(
                controller.Lines,
                l =>
                    l.Text.Contains($"Open rule {pair.First}, second rule {pair.Second}", StringComparison.Ordinal)
                    && l.Example!.OrderBy(e => e.Key, StringComparer.Ordinal)
                        .SequenceEqual(pair.Example.OrderBy(e => e.Key, StringComparer.Ordinal))
            );
        }
    }

    /// <summary>Rules that no assignment satisfies together are called unrelated.</summary>
    [Fact]
    public void FromClipboard_DisjointRules_AreUnrelated_Test()
    {
        (CompareController controller, ScriptedServices services, _) = Create("a");
        services.Clipboard.Text = "NOT a";

        controller.FromClipboard();

        Assert.Contains(controller.Lines, l => l.Text.StartsWith("The second rule is unrelated", StringComparison.Ordinal));
    }

    /// <summary>A second rule that does not compile shows its diagnostics and no comparison.</summary>
    [Fact]
    public void FromClipboard_SecondRuleDoesNotCompile_ShowsDiagnosticsAndNoComparison_Test()
    {
        (CompareController controller, ScriptedServices services, Workbench workbench) = Create("a OR b");
        services.Clipboard.Text = "a AND (";

        controller.FromClipboard();

        Assert.Contains(controller.Lines, l => l.Text.Contains("does not compile", StringComparison.Ordinal));
        Assert.Contains(
            controller.Lines,
            l => l.Text.StartsWith("  ", StringComparison.Ordinal) && l.Text.Contains(" error: ", StringComparison.Ordinal)
        );
        Assert.DoesNotContain(controller.Lines, l => l.Text.StartsWith("Overlap", StringComparison.Ordinal));
        Assert.DoesNotContain(controller.Lines, l => l.Text.StartsWith("The second rule is", StringComparison.Ordinal));
        Assert.Equal("a OR b", workbench.Document.Text);
    }

    /// <summary>An example of the pane replaces the term values of the Assign pane.</summary>
    [Fact]
    public void LoadExample_LineWithAnExample_SetsTheAssignment_Test()
    {
        (CompareController controller, ScriptedServices services, Workbench workbench) = Create("a OR b");
        services.Clipboard.Text = "a";
        controller.FromClipboard();
        CompareLine line = controller.Lines.First(l =>
            l.Text.Contains("Satisfies the open rule only", StringComparison.Ordinal)
        );

        bool loaded = controller.LoadExample(line);

        Assert.True(loaded);
        foreach ((string term, TruthValue value) in line.Example!)
        {
            Assert.Equal(value, workbench.Assignment.ValueOf(term));
        }

        Assert.Equal(TruthValue.True, workbench.Assignment.ValueOf("b"));
    }

    /// <summary>A line with no example is refused and the assignment stays.</summary>
    [Fact]
    public void LoadExample_LineWithoutAnExample_IsRefused_Test()
    {
        (CompareController controller, _, Workbench workbench) = Create("a OR b");

        bool loaded = controller.LoadExample(controller.Lines[0]);

        Assert.False(loaded);
        Assert.StartsWith("Select a line", workbench.Status, StringComparison.Ordinal);
    }

    /// <summary>A second rule from a file loads without changing or dirtying the open document.</summary>
    [Fact]
    public void FromFile_ValidRule_LoadsWithoutChangingTheOpenDocument_Test()
    {
        (CompareController controller, _, Workbench workbench) = Create("a OR b");
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".twr");
        File.WriteAllText(path, "a AND b");
        try
        {
            bool loaded = controller.FromFile(path);

            Assert.True(loaded);
            Assert.Contains(controller.Lines, l => l.Text.StartsWith("The second rule is tighter", StringComparison.Ordinal));
            Assert.Equal("a OR b", workbench.Document.Text);
            Assert.False(workbench.Document.IsDirty);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>A change of the open rule updates the comparison.</summary>
    [Fact]
    public void Lines_OpenRuleChanges_FollowTheNewRule_Test()
    {
        (CompareController controller, ScriptedServices services, Workbench workbench) = Create("a OR b");
        services.Clipboard.Text = "a";
        controller.FromClipboard();

        workbench.Document.SetText("a");

        Assert.Contains(controller.Lines, l => l.Text.StartsWith("The second rule is equivalent", StringComparison.Ordinal));
    }

    /// <summary>Close drops the second rule.</summary>
    [Fact]
    public void Close_SecondRuleLoaded_ShowsTheIdleText_Test()
    {
        (CompareController controller, ScriptedServices services, _) = Create("a OR b");
        services.Clipboard.Text = "a";
        controller.FromClipboard();

        controller.Close();

        Assert.Equal(CompareController.Idle, Assert.Single(controller.Lines).Text);
    }

    private static (CompareController Controller, ScriptedServices Services, Workbench Workbench) Create(string text)
    {
        ScriptedServices services = new();
        Workbench workbench = new(RuleDocument.Create(text), ToolSettings.Default, new ScriptedPrompts());
        return (new CompareController(workbench, services.Services), services, workbench);
    }
}
