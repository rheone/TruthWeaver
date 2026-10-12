namespace TruthWeaver.Tool.Tests;

using TruthWeaver.Tool.Tests.TestSupport;

public sealed class TruthWeaverCliTests
{
    /// <summary>--help lists every command and exits 0.</summary>
    [Fact]
    public async Task RunAsync_Help_ListsEveryCommand_Test()
    {
        CliRun run = await Cli.RunAsync(["--help"]);

        Assert.Equal(0, run.ExitCode);
        foreach (string command in new[] { "validate", "convert", "format", "print", "export", "report", "diff", "evaluate" })
        {
            Assert.Contains(command, run.Output, StringComparison.Ordinal);
        }
    }

    /// <summary>An unknown command is a usage error with exit code 2.</summary>
    [Fact]
    public async Task RunAsync_UnknownCommand_IsAUsageError_Test()
    {
        CliRun run = await Cli.RunAsync(["frobnicate"]);

        Assert.Equal(2, run.ExitCode);
        Assert.Contains("truthweaver --help", run.Error, StringComparison.Ordinal);
    }

    /// <summary>The suggest directive that shell completion uses completes command names.</summary>
    [Fact]
    public async Task RunAsync_SuggestDirective_CompletesCommandNames_Test()
    {
        CliRun run = await Cli.RunAsync(["[suggest]", "va"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Contains("validate", run.Output, StringComparison.Ordinal);
    }

    /// <summary>The suggest directive completes the artifact ids of --artifact.</summary>
    [Fact]
    public async Task RunAsync_SuggestDirectiveForArtifact_CompletesCatalogIds_Test()
    {
        CliRun run = await Cli.RunAsync(["[suggest:33]", "print access.rule --artifact merm"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Contains("mermaid", run.Output, StringComparison.Ordinal);
    }

    /// <summary>A limit below 1 is a usage error with exit code 2.</summary>
    [Fact]
    public async Task RunAsync_LimitBelowOne_IsAUsageError_Test()
    {
        CliRun run = await Cli.RunAsync(["validate", "access.rule", "--max-depth", "0"]);

        Assert.Equal(2, run.ExitCode);
        Assert.Contains("--max-depth must be a whole number from 1.", run.Error, StringComparison.Ordinal);
    }
}
