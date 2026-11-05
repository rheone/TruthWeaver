namespace TruthWeaver.Tool.Tests;

using System.Text.Json.Nodes;
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

    /// <summary>A change that adds an operand has no text before it, so the change lists a null before.</summary>
    [Fact]
    public async Task Diff_RuleWithAnAddedOperand_ListsAChangeWithNoBeforeText_Test()
    {
        using TemporaryDirectory folder = new();
        folder.Write("before.rule", "a AND b\n");
        folder.Write("after.rule", "a AND b AND c\n");

        CliRun run = await Cli.RunAsync(["diff", folder.File("before.rule"), folder.File("after.rule"), "--json"]);

        JsonArray changes = run.Json["result"]!["changes"]!.AsArray();
        Assert.Contains(changes, change => change!["before"] is null && change["after"] is not null);
    }

    /// <summary>A check against a file that differs from the first line reports line 1.</summary>
    [Fact]
    public async Task Convert_CheckAgainstAFileThatDiffersAtTheFirstLine_ReportsLineOne_Test()
    {
        using TemporaryDirectory folder = new();
        folder.Write("out.yaml", "something: else\nmore: lines\n");

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

        Assert.Equal(1, run.ExitCode);
        Assert.Equal(1, run.Json["result"]!["check"]!["firstDifferentLine"]!.GetValue<int>());
    }

    /// <summary>A rule in Mermaid that is rendered without a Mermaid program on the search path is a renderer-not-found error.</summary>
    [Fact]
    public async Task Export_RenderMermaidWithoutTheRenderer_ExitsTwo_Test()
    {
        using TemporaryDirectory folder = new();

        CliRun run = await Cli.RunAsync(
            ["export", "access.rule", "-a", "mermaid", "-o", folder.File("rule.svg"), "--render", "svg", "--json"],
            rendererSearchPath: folder.FullPath
        );

        Assert.Equal(2, run.ExitCode);
        Assert.Equal([ToolDiagnosticCodes.RendererNotFound], run.DiagnosticCodes);
    }

    /// <summary>The explicit configuration file of the import command changes the output: its symbolic operator style is used.</summary>
    [Fact]
    public async Task Import_ExplicitConfig_AppliesTheOperatorStyleOfThatFile_Test()
    {
        const string Input = """{"and": [{"var": "a"}, {"var": "b"}]}""";

        CliRun withConfig = await Cli.RunAsync(["import", "--config", "config/truthweaver.json"], standardInput: Input);
        CliRun withoutConfig = await Cli.RunAsync(["import"], standardInput: Input);

        Assert.Equal(0, withConfig.ExitCode);
        Assert.Equal(0, withoutConfig.ExitCode);
        Assert.NotEqual(withoutConfig.Output, withConfig.Output);
    }

    /// <summary>The test command reads an explicit configuration file, so a file that does not exist is an error.</summary>
    [Fact]
    public async Task Test_ExplicitConfigThatDoesNotExist_ExitsTwoWithInputUnreadable_Test()
    {
        CliRun run = await Cli.RunAsync(["test", "access.test.yaml", "--config", "config/none.json", "--json"]);

        Assert.Equal(2, run.ExitCode);
        Assert.Contains(ToolDiagnosticCodes.InputUnreadable, run.DiagnosticCodes);
    }

    /// <summary>A check of every artifact of a rule that does not compile reports the compile error and checks no file.</summary>
    [Fact]
    public async Task Export_AllCheckOfARuleThatDoesNotCompile_ReportsTheErrorAndChecksNothing_Test()
    {
        using TemporaryDirectory folder = new();

        CliRun run = await Cli.RunAsync(["export", "broken.rule", "--all", folder.FullPath, "--check", "--json"]);

        Assert.Equal(1, run.ExitCode);
        Assert.Equal(["TRE0001"], run.DiagnosticCodes);
    }

    /// <summary>An output path under a file cannot be written, and the command exits 2 with the output-failed code.</summary>
    [Fact]
    public async Task Format_OutputPathUnderAFile_ExitsTwoWithOutputFailed_Test()
    {
        using TemporaryDirectory folder = new();
        folder.Write("blocker", "x");

        CliRun run = await Cli.RunAsync([
            "format",
            "access.rule",
            "--output",
            Path.Combine(folder.File("blocker"), "out.rule"),
            "--json",
        ]);

        Assert.Equal(2, run.ExitCode);
        Assert.Equal([ToolDiagnosticCodes.OutputFailed], run.DiagnosticCodes);
    }

    /// <summary>A changed predicate argument is listed with the predicate name and its arguments on both sides.</summary>
    [Fact]
    public async Task Diff_ChangedPredicateArgument_ListsTheArgumentsOnBothSides_Test()
    {
        using TemporaryDirectory folder = new();
        folder.Write("before.rule", "hasRole(role: \"editor\")\n");
        folder.Write("after.rule", "hasRole(role: \"admin\")\n");

        CliRun run = await Cli.RunAsync(["diff", folder.File("before.rule"), folder.File("after.rule"), "--json"]);

        JsonArray changes = run.Json["result"]!["changes"]!.AsArray();
        Assert.Contains(changes, change => change!["before"]!.GetValue<string>().Contains("editor", StringComparison.Ordinal));
        Assert.Contains(changes, change => change!["after"]!.GetValue<string>().Contains("admin", StringComparison.Ordinal));
    }

    /// <summary>A check of rule text from standard input compares it with the formatted text and reports the first line that differs.</summary>
    [Fact]
    public async Task Format_CheckOfStandardInputThatIsNotFormatted_ReportsLineOne_Test()
    {
        CliRun run = await Cli.RunAsync(["format", "-", "--check", "--json"], standardInput: "isActive   AND  isBanned\n");

        Assert.Equal(1, run.ExitCode);
        Assert.Equal(1, run.Json["result"]!["check"]!["firstDifferentLine"]!.GetValue<int>());
    }
}
