namespace TruthWeaver.Tool.Tests;

using System.Xml.Linq;
using TruthWeaver.Tool.Core;
using TruthWeaver.Tool.Tests.TestSupport;

public sealed class TestCommandTests
{
    private const string FailingFile = """
        rule: isActive
        cases:
          - name: passes
            given: { isActive: True }
            expect: { result: True }
          - name: fails
            given: { isActive: True }
            expect: { result: False }
        """;

    /// <summary>A file whose cases all pass lists each case and exits with 0.</summary>
    [Fact]
    public async Task Test_EveryCasePasses_ListsTheCasesAndExitsZero_Test()
    {
        CliRun run = await Cli.RunAsync(["test", "access.test.yaml"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Equal(
            "PASS an active editor is let in\nPASS an inactive editor is kept out\nPASS an unknown role is undecided\n"
                + "3 case(s): 3 passed, 0 failed.\n",
            run.Output
        );
    }

    /// <summary>A failing case gives exit code 1, and the output names the case and the reason.</summary>
    [Fact]
    public async Task Test_FailingCase_ExitsOne_Test()
    {
        using TemporaryDirectory folder = new();
        string file = folder.Write("failing.test.yaml", FailingFile);

        CliRun run = await Cli.RunAsync(["test", file]);

        Assert.Equal(1, run.ExitCode);
        Assert.Contains(
            "FAIL fails\n  Expected the result False, but the result is True.\n",
            run.Output,
            StringComparison.Ordinal
        );
    }

    /// <summary>The JSON envelope has the counts and one entry per case.</summary>
    [Fact]
    public async Task Test_Json_HoldsTheCountsAndTheCases_Test()
    {
        using TemporaryDirectory folder = new();
        string file = folder.Write("failing.test.yaml", FailingFile);

        CliRun run = await Cli.RunAsync(["test", file, "--json"]);

        Assert.Equal(1, run.Json["exitCode"]!.GetValue<int>());
        Assert.Equal(1, run.Json["result"]!["passed"]!.GetValue<int>());
        Assert.Equal(1, run.Json["result"]!["failed"]!.GetValue<int>());
        Assert.Equal("True", run.Json["result"]!["cases"]![1]!["result"]!.GetValue<string>());
        Assert.Equal("False", run.Json["result"]!["cases"]![1]!["expected"]!.GetValue<string>());
    }

    /// <summary>--junit writes a JUnit report with one test case per case and the failure.</summary>
    [Fact]
    public async Task Test_JUnit_WritesTheReport_Test()
    {
        using TemporaryDirectory folder = new();
        string file = folder.Write("failing.test.yaml", FailingFile);
        string junit = folder.File("out/junit.xml");

        CliRun run = await Cli.RunAsync(["test", file, "--junit", junit]);

        Assert.Equal(1, run.ExitCode);
        XElement suite = XDocument.Load(junit).Root!.Element("testsuite")!;
        Assert.Equal("failing.test", suite.Attribute("name")!.Value);
        Assert.Equal(2, suite.Elements("testcase").Count());
        Assert.Single(suite.Descendants("failure"));
    }

    /// <summary>An unusable test file is a TWT0022 error with exit code 2.</summary>
    [Fact]
    public async Task Test_InvalidFile_ExitsTwo_Test()
    {
        using TemporaryDirectory folder = new();
        string file = folder.Write("invalid.test.yaml", "rule: isActive\ncases:\n  - name: x\n    expect: { result: maybe }\n");

        CliRun run = await Cli.RunAsync(["test", file, "--json"]);

        Assert.Equal(2, run.ExitCode);
        Assert.Equal([ToolDiagnosticCodes.InvalidRuleTestFile], run.DiagnosticCodes);
    }

    /// <summary>
    /// A rule that does not compile fails every case with exit code 1, and the compile error is a diagnostic. An inline
    /// rule is an embedded rule at <c>$.rule</c>, so its positions count in the rule text.
    /// </summary>
    [Fact]
    public async Task Test_RuleDoesNotCompile_ExitsOneWithTheCompileError_Test()
    {
        using TemporaryDirectory folder = new();
        string file = folder.Write(
            "broken.test.yaml",
            "rule: isActive AND\ncases:\n  - { name: x, expect: { result: True } }\n"
        );

        CliRun run = await Cli.RunAsync(["test", file, "--json"]);

        Assert.Equal(1, run.ExitCode);
        Assert.NotEmpty(run.DiagnosticCodes);
        Assert.All(run.DiagnosticCodes, code => Assert.StartsWith("TRE", code, StringComparison.Ordinal));
        Assert.All(run.Json["diagnostics"]!.AsArray(), d => Assert.Equal("$.rule", d!["rulePath"]!.GetValue<string>()));
    }
}
