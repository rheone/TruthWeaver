namespace TruthWeaver.Tool.Tests;

using TruthWeaver.Tool.Tests.TestSupport;

public sealed class ImportCommandTests
{
    /// <summary>A JsonLogic rule on standard input is written as rule text, and the warnings go to standard error with exit code 0.</summary>
    [Fact]
    public async Task Import_JsonLogicOnStandardInput_WritesRuleTextAndWarnings_Test()
    {
        CliRun run = await Cli.RunAsync(
            ["import"],
            standardInput: """{"and": [{">=": [{"var": "user.age"}, 18]}, {"!": {"var": "user.banned"}}]}"""
        );

        Assert.Equal(0, run.ExitCode);
        Assert.Equal("userAgeIsGreaterThanOrEqualTo(value: 18) AND NOT userBanned\n", run.Output);
        Assert.Contains("TWT0024", run.Error, StringComparison.Ordinal);
    }

    /// <summary>--to json writes the rule tree, and --json wraps the result in the envelope with the diagnostics.</summary>
    [Fact]
    public async Task Import_ToJsonWithEnvelope_ReportsFormatAndDiagnostics_Test()
    {
        CliRun run = await Cli.RunAsync(["import", "--to", "json", "--json"], standardInput: """{"===": [{"var": "a"}, 1]}""");

        Assert.Equal(0, run.ExitCode);
        Assert.Equal("import", run.Json["command"]!.GetValue<string>());
        Assert.Equal("jsonlogic", run.Json["result"]!["from"]!.GetValue<string>());
        Assert.Equal("json", run.Json["result"]!["format"]!.GetValue<string>());
        Assert.Contains("aIsEqualTo", run.Json["result"]!["text"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    /// <summary>An unsupported operator writes nothing and exits with code 2.</summary>
    [Fact]
    public async Task Import_UnsupportedOperator_WritesNothingAndExitsWithTwo_Test()
    {
        CliRun run = await Cli.RunAsync(["import"], standardInput: """{"cat": ["a", "b"]}""");

        Assert.Equal(2, run.ExitCode);
        Assert.Empty(run.Output);
        Assert.Contains("TWT0023", run.Error, StringComparison.Ordinal);
    }

    /// <summary>--output writes the file and --check then finds it identical.</summary>
    [Fact]
    public async Task Import_OutputAndCheck_WritesThenComparesTheFile_Test()
    {
        using TemporaryDirectory folder = new();
        string output = folder.File("rule.txt");
        const string Input = """{"!": {"===": [{"var": "a"}, null]}}""";

        CliRun written = await Cli.RunAsync(["import", "--output", output], standardInput: Input);
        CliRun checkedRun = await Cli.RunAsync(["import", "--output", output, "--check"], standardInput: Input);

        Assert.Equal(0, written.ExitCode);
        Assert.Equal("NOT aIsNull\n", await File.ReadAllTextAsync(output, TestContext.Current.CancellationToken));
        Assert.Equal(0, checkedRun.ExitCode);
        Assert.Equal($"{output}: identical\n", checkedRun.Output);
    }
}
