namespace TruthWeaver.Tool.Tests;

using TruthWeaver.Tool.Tests.TestSupport;

public sealed class FormatCommandTests
{
    /// <summary>A rule in canonical form passes --check against itself, with exit code 0.</summary>
    [Fact]
    public async Task Format_CheckOfAFormattedFile_IsIdentical_Test()
    {
        CliRun run = await Cli.RunAsync(["format", "access.rule", "--check"]);

        Assert.Equal(0, run.ExitCode);
        Assert.EndsWith("access.rule: identical\n", run.Output, StringComparison.Ordinal);
    }

    /// <summary>A rule that is not in canonical form fails --check with the first line that differs, and the file does not change.</summary>
    [Fact]
    public async Task Format_CheckOfAnUnformattedFile_FailsWithExitCodeOne_Test()
    {
        string before = await File.ReadAllTextAsync(Samples.Path("access.json"), TestContext.Current.CancellationToken);

        CliRun run = await Cli.RunAsync(["format", "access.json", "--check"]);

        Assert.Equal(1, run.ExitCode);
        Assert.EndsWith("access.json: differs at line 1\n", run.Output, StringComparison.Ordinal);
        Assert.Equal(before, await File.ReadAllTextAsync(Samples.Path("access.json"), TestContext.Current.CancellationToken));
    }

    /// <summary>The nearest truthweaver.json gives the operator style; the command line wins over it.</summary>
    [Fact]
    public async Task Format_ConfigurationFile_GivesTheOperatorStyleUnlessOverridden_Test()
    {
        CliRun fromConfig = await Cli.RunAsync(["format", "config/rule.rule"]);
        CliRun fromCommandLine = await Cli.RunAsync(["format", "config/rule.rule", "--operator-style", "word"]);

        Assert.Equal("isActive ∧ ¬isBanned\n", fromConfig.Output);
        Assert.Equal("isActive AND NOT isBanned\n", fromCommandLine.Output);
    }

    /// <summary>--config names the configuration file explicitly.</summary>
    [Fact]
    public async Task Format_ExplicitConfig_UsesThatFile_Test()
    {
        CliRun run = await Cli.RunAsync(["format", "active.rule", "--config", "config/truthweaver.json", "--json"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Equal("isActive\n", run.Json["result"]!["text"]!.GetValue<string>());
    }

    /// <summary>--pretty indents JSON by two spaces.</summary>
    [Fact]
    public async Task Format_PrettyJson_IndentsTheTree_Test()
    {
        CliRun run = await Cli.RunAsync(["format", "access.json", "--pretty"]);

        Assert.Equal(0, run.ExitCode);
        Assert.StartsWith("{\n  \"op\": \"and\",\n  \"operands\": [\n", run.Output, StringComparison.Ordinal);
    }

    /// <summary>An embedded rule is formatted and checked as text, not against the whole document.</summary>
    [Fact]
    public async Task Format_CheckOfAnEmbeddedRule_ComparesTheRuleText_Test()
    {
        CliRun run = await Cli.RunAsync(["format", "policies.yaml", "--path", "rules.review", "--check", "--json"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Equal("identical", run.Json["result"]!["check"]!["status"]!.GetValue<string>());
    }
}
