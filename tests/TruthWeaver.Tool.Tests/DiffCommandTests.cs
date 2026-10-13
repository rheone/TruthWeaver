namespace TruthWeaver.Tool.Tests;

using TruthWeaver.Tool.Tests.TestSupport;

public sealed class DiffCommandTests
{
    /// <summary>The same rule in rule text and in JSON has no changes, is equivalent, and exits 0.</summary>
    [Fact]
    public async Task Diff_SameRuleInTwoFormats_IsEquivalent_Test()
    {
        CliRun run = await Cli.RunAsync(["diff", "access.rule", "access.json"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Equal("No changes.\nStrong K3 equivalence: equivalent\nRelation: equivalent\n", run.Output);
    }

    /// <summary>Dropping a conjunct makes the after rule looser: it is not equivalent (exit 1), with an example that only the after rule accepts.</summary>
    [Fact]
    public async Task Diff_DroppedConjunct_IsLooserAndExitsOne_Test()
    {
        CliRun run = await Cli.RunAsync(["diff", "access.rule", "active.rule", "--json"]);

        Assert.Equal(1, run.ExitCode);
        Assert.False(run.Json["result"]!["equivalent"]!.GetValue<bool>());
        Assert.Equal("looser", run.Json["result"]!["relation"]!.GetValue<string>());
        Assert.Equal("True", run.Json["result"]!["satisfiesOnlyAfter"]!["isActive"]!.GetValue<string>());
        Assert.NotEmpty(run.Json["result"]!["changes"]!.AsArray());
    }

    /// <summary>The reverse change is tighter.</summary>
    [Fact]
    public async Task Diff_AddedConjunct_IsTighter_Test()
    {
        CliRun run = await Cli.RunAsync(["diff", "active.rule", "access.rule"]);

        Assert.Equal(1, run.ExitCode);
        Assert.Contains("Relation: tighter", run.Output, StringComparison.Ordinal);
    }

    /// <summary>The before rule can come from standard input.</summary>
    [Fact]
    public async Task Diff_StandardInput_IsTheBeforeRule_Test()
    {
        CliRun run = await Cli.RunAsync(["diff", "-", "active.rule"], standardInput: "isActive");

        Assert.Equal(0, run.ExitCode);
    }
}
