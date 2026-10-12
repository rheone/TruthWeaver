namespace TruthWeaver.Tool.Tests;

using TruthWeaver.Tool.Tests.TestSupport;

public sealed class ReportCommandTests
{
    /// <summary>The report is Markdown with the rule, the equation, the diagram, the truth table and the statistics.</summary>
    [Fact]
    public async Task Report_Rule_WritesTheMarkdownSections_Test()
    {
        CliRun run = await Cli.RunAsync(["report", "access.rule"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Contains("```mermaid", run.Output, StringComparison.Ordinal);
        Assert.Contains("Truth table", run.Output, StringComparison.Ordinal);
        Assert.Contains("Statistics", run.Output, StringComparison.Ordinal);
    }

    /// <summary>The report written with --output passes --check, and the JSON envelope names the file.</summary>
    [Fact]
    public async Task Report_OutputThenCheck_IsIdentical_Test()
    {
        using TemporaryDirectory folder = new();
        string output = folder.File("report.md");

        await Cli.RunAsync(["report", "access.rule", "-o", output]);
        CliRun run = await Cli.RunAsync(["report", "access.rule", "-o", output, "--check", "--json"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Equal(output, run.Json["result"]!["output"]!.GetValue<string>());
        Assert.Equal("identical", run.Json["result"]!["check"]!["status"]!.GetValue<string>());
    }
}
