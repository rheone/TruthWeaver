namespace TruthWeaver.Tool.Tests;

using System.Reflection;
using TruthWeaver.Tool.Core.Export;
using TruthWeaver.Tool.Tests.TestSupport;

/// <summary>
/// Keeps the command line level with the export dialog of the workbench: the same artifacts and the same
/// <see cref="ExportOptions"/>. The workbench side of this check is <c>ExportParityTests</c> in the workbench tests.
/// </summary>
public sealed class ExportParityTests
{
    private const string NineTerms = "p1 AND p2 AND p3 AND p4 AND p5 AND p6 AND p7 AND p8 AND p9";

    /// <summary>
    /// How the command line reaches each <see cref="ExportOptions"/> property. A new property fails the test until it is
    /// exposed here and in the workbench, or listed as front-end specific.
    /// </summary>
    private static readonly Dictionary<string, string> Exposed = new(StringComparer.Ordinal)
    {
        [nameof(ExportOptions.Settings)] = "--config and the other settings options",
        [nameof(ExportOptions.AllowLargerTruthTable)] = "--allow-large-truth-table",
        [nameof(ExportOptions.TruthTableTermLimit)] = "--truth-table-limit",
        [nameof(ExportOptions.Images)] = "--render",
    };

    /// <summary>The choices that only the command line has, so the workbench does not need them.</summary>
    private static readonly string[] CommandLineOnly = ["--output", "--check", "--all", "--json", "standard output"];

    /// <summary>Every artifact id of the catalog is accepted by <c>--artifact</c>.</summary>
    [Fact]
    public async Task Export_EveryCatalogArtifact_IsAcceptedByArtifact_Test()
    {
        using TemporaryDirectory folder = new();
        List<string> refused = [];

        foreach (ArtifactDefinition artifact in ArtifactCatalog.All)
        {
            CliRun run = await Cli.RunAsync([
                "export",
                "access.rule",
                "--artifact",
                artifact.Id,
                "--output",
                folder.File(artifact.FileName),
                "--truth-table-limit",
                "12",
            ]);
            if (run.ExitCode != 0)
            {
                refused.Add($"{artifact.Id} ({run.ExitCode})");
            }
        }

        Assert.Empty(refused);
    }

    /// <summary>Every property of <see cref="ExportOptions"/> has an option on the command line, and the list has no stale name.</summary>
    [Fact]
    public async Task ExportOptions_EveryProperty_IsExposedByTheCommandLine_Test()
    {
        string[] properties =
        [
            .. typeof(ExportOptions)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.Name != "EqualityContract")
                .Select(p => p.Name),
        ];
        CliRun export = await Cli.RunAsync(["export", "--help"]);
        CliRun print = await Cli.RunAsync(["print", "--help"]);
        CliRun report = await Cli.RunAsync(["report", "--help"]);

        Assert.Equal(properties.Order(StringComparer.Ordinal), Exposed.Keys.Order(StringComparer.Ordinal));
        Assert.Contains("--render", export.Output, StringComparison.Ordinal);
        foreach (CliRun help in new[] { export, print, report })
        {
            Assert.Contains("--allow-large-truth-table", help.Output, StringComparison.Ordinal);
            Assert.Contains("--truth-table-limit", help.Output, StringComparison.Ordinal);
        }

        Assert.NotEmpty(CommandLineOnly);
    }

    /// <summary>A rule over the cap is refused, and <c>--truth-table-limit</c> lets it through.</summary>
    [Fact]
    public async Task Print_TruthTableLimit_AllowsATableOverTheCap_Test()
    {
        CliRun refused = await Cli.RunAsync(["print", "-a", "truth-table-csv"], NineTerms);
        CliRun allowed = await Cli.RunAsync(["print", "-a", "truth-table-csv", "--truth-table-limit", "9"], NineTerms);

        Assert.NotEqual(0, refused.ExitCode);
        Assert.Equal(0, allowed.ExitCode);
        Assert.NotEmpty(allowed.Output);
    }

    /// <summary>The older <c>--allow-large-truth-table</c> still works, and it means the limit of 12.</summary>
    [Fact]
    public async Task Print_AllowLargeTruthTable_StillAllowsATableOfNineTerms_Test()
    {
        CliRun run = await Cli.RunAsync(["print", "-a", "truth-table-csv", "--allow-large-truth-table"], NineTerms);

        Assert.Equal(0, run.ExitCode);
    }

    /// <summary>A limit below 1 is a usage error.</summary>
    [Fact]
    public async Task Print_TruthTableLimitBelowOne_IsAUsageError_Test()
    {
        CliRun run = await Cli.RunAsync(["print", "-a", "truth-table-csv", "--truth-table-limit", "0"], "a AND b");

        Assert.NotEqual(0, run.ExitCode);
    }
}
