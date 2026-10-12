namespace TruthWeaver.Tool.Tests;

using TruthWeaver.Tool.Core;
using TruthWeaver.Tool.Tests.TestSupport;

public sealed class EvaluateCommandTests
{
    /// <summary>The stub values decide the result.</summary>
    [Fact]
    public async Task Evaluate_StubValues_GiveTheResult_Test()
    {
        CliRun run = await Cli.RunAsync(["evaluate", "access.rule", "--stubs", "stubs.yaml"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Equal("Result: False\n", run.Output);
    }

    /// <summary>Without stub values every term is Unknown; --trace adds the trace tree.</summary>
    [Fact]
    public async Task Evaluate_NoStubsWithTrace_IsUnknownWithTheTree_Test()
    {
        CliRun run = await Cli.RunAsync(["evaluate", "access.rule", "--trace"]);

        Assert.Equal(0, run.ExitCode);
        Assert.StartsWith("Result: Unknown\nAND [unknown]\n", run.Output, StringComparison.Ordinal);
    }

    /// <summary>A data file resolves a from(...) reference, and the stub of the resolved term applies.</summary>
    [Fact]
    public async Task Evaluate_DataFile_ResolvesTheReferenceForTheStub_Test()
    {
        using TemporaryDirectory folder = new();
        string stubs = folder.Write("stubs.yaml", "isActive: T\n'inRegion(region: \"EU\")': T\n");

        CliRun run = await Cli.RunAsync(["evaluate", "region.rule", "--stubs", stubs, "--data", "user=user.json"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Equal("Result: True\n", run.Output);
    }

    /// <summary>A reference to a source with no data file is a fault, shown in the output.</summary>
    [Fact]
    public async Task Evaluate_MissingDataSource_ShowsTheFault_Test()
    {
        CliRun run = await Cli.RunAsync(["evaluate", "region.rule"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Contains("Fault: inRegion(region: from(\"user\", \"$.region\"))", run.Output, StringComparison.Ordinal);
    }

    /// <summary>The JSON envelope holds the result and the decision record, with the trace.</summary>
    [Fact]
    public async Task Evaluate_Json_HoldsTheDecisionRecord_Test()
    {
        CliRun run = await Cli.RunAsync(["evaluate", "access.rule", "--stubs", "stubs.yaml", "--exhaustive", "--json"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Equal("False", run.Json["result"]!["result"]!.GetValue<string>());
        Assert.Equal("Exhaustive", run.Json["result"]!["decision"]!["options"]!["mode"]!.GetValue<string>());
        Assert.Equal(2, run.Json["result"]!["decision"]!["trace"]!.AsArray().Count);
    }

    /// <summary>A stub file with an invalid value is a TWT0009 error with exit code 2.</summary>
    [Fact]
    public async Task Evaluate_InvalidStubFile_ExitsTwo_Test()
    {
        using TemporaryDirectory folder = new();
        string stubs = folder.Write("stubs.yaml", "isActive: maybe\n");

        CliRun run = await Cli.RunAsync(["evaluate", "access.rule", "--stubs", stubs, "--json"]);

        Assert.Equal(2, run.ExitCode);
        Assert.Equal([ToolDiagnosticCodes.InvalidStubValue], run.DiagnosticCodes);
    }

    /// <summary>--data without a name is a usage error with exit code 2.</summary>
    [Fact]
    public async Task Evaluate_DataWithoutName_IsAUsageError_Test()
    {
        CliRun run = await Cli.RunAsync(["evaluate", "region.rule", "--data", "user.json"]);

        Assert.Equal(2, run.ExitCode);
        Assert.Contains("--data takes name=file", run.Error, StringComparison.Ordinal);
    }
}
