namespace TruthWeaver.Tool.Tests;

using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Tests.TestSupport;

/// <summary>The error arms and option combinations of the diff, test, import and export commands.</summary>
public sealed class CommandEdgeCaseTests
{
    /// <summary>Two rules from standard input are a usage error (exit 2) before any rule is read.</summary>
    [Fact]
    public async Task Diff_BothRulesFromStandardInput_IsAUsageError_Test()
    {
        CliRun run = await Cli.RunAsync(["diff", "-", "-"], standardInput: "a");

        Assert.Equal(2, run.ExitCode);
        Assert.Contains("Only one of the two rules can come from standard input", run.Error, StringComparison.Ordinal);
    }

    /// <summary>A before file that does not exist is an error with exit code 2 and no comparison.</summary>
    [Fact]
    public async Task Diff_BeforeFileMissing_ExitsTwoWithoutAComparison_Test()
    {
        CliRun run = await Cli.RunAsync(["diff", "no-such-file.rule", "active.rule", "--json"]);

        Assert.Equal(2, run.ExitCode);
        Assert.Equal(["TWT0001"], run.DiagnosticCodes);
        Assert.Null(run.Json["result"]?["relation"]);
    }

    /// <summary>An after rule that does not compile reports its error and does not compare, even though the before rule is fine.</summary>
    [Fact]
    public async Task Diff_AfterRuleDoesNotCompile_ReportsTheErrorAndDoesNotCompare_Test()
    {
        CliRun run = await Cli.RunAsync(["diff", "active.rule", "broken.rule", "--json"]);

        Assert.Equal(1, run.ExitCode);
        Assert.Equal(["TRE0001"], run.DiagnosticCodes);
        Assert.Null(run.Json["result"]?["relation"]);
    }

    /// <summary>When the analysis limit is below the term count, the comparison is undecided, exits 1, and gives the reason.</summary>
    [Fact]
    public async Task Diff_MoreTermsThanTheAnalysisLimit_IsUndecidedWithAReason_Test()
    {
        CliRun run = await Cli.RunAsync(["diff", "access.rule", "active.rule", "--max-analysis-terms", "1", "--json"]);

        Assert.Equal(1, run.ExitCode);
        Assert.Null(run.Json["result"]!["equivalent"]);
        Assert.NotNull(run.Json["result"]!["reason"]);
    }

    /// <summary>A tighter after rule lists an example that only the before rule accepts, in the text output.</summary>
    [Fact]
    public async Task Diff_TighterAfterRule_ListsTheExampleOnlyTheBeforeRuleAccepts_Test()
    {
        CliRun run = await Cli.RunAsync(["diff", "active.rule", "access.rule"]);

        Assert.Contains("True only for the before rule: ", run.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("True only for the after rule", run.Output, StringComparison.Ordinal);
    }

    /// <summary>A test file that does not exist is a TWT0022 error with exit code 2.</summary>
    [Fact]
    public async Task Test_FileMissing_ExitsTwoWithAnInvalidTestFileError_Test()
    {
        CliRun run = await Cli.RunAsync(["test", "no-such.test.yaml", "--json"]);

        Assert.Equal(2, run.ExitCode);
        Assert.Contains(ToolDiagnosticCodes.InvalidRuleTestFile, run.DiagnosticCodes);
    }

    /// <summary>A case that expects a number of faults that the run does not have fails, and the output says which.</summary>
    [Fact]
    public async Task Test_ExpectedFaultsDiffer_FailsTheCase_Test()
    {
        using TemporaryDirectory folder = new();
        string file = folder.Write(
            "faults.test.yaml",
            "rule: isActive\ncases:\n  - name: counts faults\n    given: { isActive: True }\n    expect: { result: True, faults: 2 }\n"
        );

        CliRun run = await Cli.RunAsync(["test", file]);

        Assert.Equal(1, run.ExitCode);
        Assert.Contains(
            "FAIL counts faults\n  Expected 2 fault(s), but the evaluation recorded 0.\n",
            run.Output,
            StringComparison.Ordinal
        );
    }

    /// <summary>A JUnit path that is an existing directory is reported as a TWT0010 error with exit code 2, not as an unhandled exception.</summary>
    [Fact]
    public async Task Test_JUnitPathIsADirectory_ReportsADiagnostic_Test()
    {
        using TemporaryDirectory folder = new();

        CliRun run = await Cli.RunAsync(["test", "access.test.yaml", "--junit", folder.FullPath, "--json"]);

        Assert.Equal(2, run.ExitCode);
        Assert.Equal(["TWT0010"], run.DiagnosticCodes);
    }

    /// <summary>An input file that does not exist is an error with exit code 2.</summary>
    [Fact]
    public async Task Import_InputFileMissing_ExitsTwo_Test()
    {
        CliRun run = await Cli.RunAsync(["import", "no-such.json", "--json"]);

        Assert.Equal(2, run.ExitCode);
        Assert.Equal(["TWT0001"], run.DiagnosticCodes);
    }

    /// <summary>JsonLogic that is not JSON is a malformed-document error with exit code 2.</summary>
    [Fact]
    public async Task Import_MalformedJson_ReportsAMalformedDocument_Test()
    {
        CliRun run = await Cli.RunAsync(["import", "--json"], standardInput: "{");

        Assert.Equal(2, run.ExitCode);
        Assert.Contains(ToolDiagnosticCodes.MalformedDocument, run.DiagnosticCodes);
    }

    /// <summary>A rule that is imported to YAML is written as YAML.</summary>
    [Fact]
    public async Task Import_ToYaml_WritesYaml_Test()
    {
        CliRun run = await Cli.RunAsync(["import", "--to", "yaml"], standardInput: """{"and": [true, false]}""");

        Assert.Equal(0, run.ExitCode);
        Assert.StartsWith("op: and\noperands:\n- const: true\n- const: false\n", run.Output, StringComparison.Ordinal);
    }

    /// <summary>A check against a file that holds a longer text reports the first line that the new output does not have.</summary>
    [Fact]
    public async Task Convert_CheckAgainstALongerFile_ReportsTheFirstExtraLine_Test()
    {
        using TemporaryDirectory folder = new();
        CliRun written = await Cli.RunAsync(["convert", "access.rule", "--to", "yaml", "--output", folder.File("out.yaml")]);
        string existing = await File.ReadAllTextAsync(folder.File("out.yaml"), TestContext.Current.CancellationToken);
        int lines = existing.TrimEnd('\n').Split('\n').Length;
        await File.WriteAllTextAsync(
            folder.File("out.yaml"),
            existing + "extra: line\n",
            TestContext.Current.CancellationToken
        );

        CliRun run = await Cli.RunAsync([
            "convert",
            "access.rule",
            "--to",
            "yaml",
            "--output",
            folder.File("out.yaml"),
            "--check",
            "--json",
        ]);

        Assert.Equal(0, written.ExitCode);
        Assert.Equal(1, run.ExitCode);
        Assert.Equal(lines + 1, run.Json["result"]!["check"]!["firstDifferentLine"]!.GetValue<int>());
    }

    /// <summary>An artifact name that the catalog does not have is a usage error.</summary>
    [Fact]
    public async Task Export_UnknownArtifact_IsAUsageError_Test()
    {
        CliRun run = await Cli.RunAsync(["export", "access.rule", "--artifact", "nope", "--output", "x.txt"]);

        Assert.Equal(2, run.ExitCode);
    }
}
