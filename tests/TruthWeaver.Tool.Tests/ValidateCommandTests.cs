namespace TruthWeaver.Tool.Tests;

using System.Text.Json.Nodes;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Tests.TestSupport;

public sealed class ValidateCommandTests
{
    /// <summary>A valid rule passes every level that applies, the report has the statistics, and the exit code is 0.</summary>
    [Fact]
    public async Task Validate_ValidRule_ReportsTheLevelsAndExitsZero_Test()
    {
        CliRun run = await Cli.RunAsync(["validate", "access.rule"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Contains("  compile: passed", run.Output, StringComparison.Ordinal);
        Assert.Contains("  nodes: 3", run.Output, StringComparison.Ordinal);
        Assert.Empty(run.Error);
    }

    /// <summary>A rule that does not parse fails the compile level with its diagnostic, and the exit code is 1.</summary>
    [Fact]
    public async Task Validate_RuleWithASyntaxError_FailsWithExitCodeOne_Test()
    {
        CliRun run = await Cli.RunAsync(["validate", "broken.rule"]);

        Assert.Equal(1, run.ExitCode);
        Assert.Contains("  compile: failed", run.Output, StringComparison.Ordinal);
        Assert.Contains("TRE0001 error at line 2, column 1", run.Output, StringComparison.Ordinal);
    }

    /// <summary>The JSON envelope names the command and the exit code, and holds the validation report.</summary>
    [Fact]
    public async Task Validate_Json_WritesTheEnvelopeWithTheReport_Test()
    {
        CliRun run = await Cli.RunAsync(["validate", "access.yaml", "--json"]);

        Assert.Equal(0, run.ExitCode);
        JsonObject json = run.Json;
        Assert.Equal("validate", json["command"]!.GetValue<string>());
        Assert.Equal(0, json["exitCode"]!.GetValue<int>());
        Assert.Empty(json["diagnostics"]!.AsArray());
        JsonNode report = json["result"]!["report"]!;
        Assert.Equal(3, report["statistics"]!["nodeCount"]!.GetValue<int>());
    }

    /// <summary>SARIF output is a SARIF 2.1.0 log with a result for each diagnostic.</summary>
    [Fact]
    public async Task Validate_Sarif_WritesASarifLog_Test()
    {
        CliRun run = await Cli.RunAsync(["validate", "broken.rule", "--sarif"]);

        Assert.Equal(1, run.ExitCode);
        JsonObject log = JsonNode.Parse(run.Output)!.AsObject();
        Assert.Equal("2.1.0", log["version"]!.GetValue<string>());
        JsonArray results = log["runs"]![0]!["results"]!.AsArray();
        Assert.Equal("TRE0001", results[0]!["ruleId"]!.GetValue<string>());
    }

    /// <summary>A warning passes without --strict and fails with it: the unknown configuration property is a TWT0008 warning.</summary>
    [Fact]
    public async Task Validate_StrictWithAWarning_FailsOnlyInStrictMode_Test()
    {
        CliRun lenient = await Cli.RunAsync(["validate", "strict/rule.rule"]);
        CliRun strict = await Cli.RunAsync(["validate", "strict/rule.rule", "--strict"]);

        Assert.Equal(0, lenient.ExitCode);
        Assert.Contains(ToolDiagnosticCodes.UnknownConfigurationProperty, lenient.Error, StringComparison.Ordinal);
        Assert.Equal(1, strict.ExitCode);
    }

    /// <summary>A level given to --skip is reported as off.</summary>
    [Fact]
    public async Task Validate_SkipLevel_ReportsTheLevelAsOff_Test()
    {
        CliRun run = await Cli.RunAsync(["validate", "access.rule", "--skip", "analysis", "--skip", "conversion-check"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Contains("  analysis: off", run.Output, StringComparison.Ordinal);
        Assert.Contains("  conversionCheck: off", run.Output, StringComparison.Ordinal);
    }

    /// <summary>The predicate schema file reports a predicate that it does not declare.</summary>
    [Fact]
    public async Task Validate_SchemaFile_ReportsAnUndeclaredPredicate_Test()
    {
        CliRun run = await Cli.RunAsync(["validate", "region.rule", "--schema-file", "predicates.yaml"]);

        Assert.Equal(1, run.ExitCode);
        Assert.Contains("  predicateSchema: failed", run.Output, StringComparison.Ordinal);
        Assert.Contains("TRE0002", run.Output, StringComparison.Ordinal);
    }

    /// <summary>A file that does not exist is a TWT0001 error, and the exit code is 2: the tool cannot use the input.</summary>
    [Fact]
    public async Task Validate_MissingFile_ExitsTwoWithTheInputError_Test()
    {
        CliRun run = await Cli.RunAsync(["validate", "no-such-file.rule", "--json"]);

        Assert.Equal(2, run.ExitCode);
        Assert.Equal([ToolDiagnosticCodes.InputUnreadable], run.DiagnosticCodes);
    }

    /// <summary>--sarif and --json together are a usage error with exit code 2.</summary>
    [Fact]
    public async Task Validate_SarifAndJson_IsAUsageError_Test()
    {
        CliRun run = await Cli.RunAsync(["validate", "access.rule", "--sarif", "--json"]);

        Assert.Equal(2, run.ExitCode);
        Assert.Contains("--sarif and --json cannot be used together.", run.Error, StringComparison.Ordinal);
    }
}
