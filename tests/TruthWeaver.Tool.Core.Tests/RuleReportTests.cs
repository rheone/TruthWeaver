namespace TruthWeaver.Tool.Core.Tests;

using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Tests.TestSupport;

/// <summary>The Markdown report has the rule, the equation, a diagram, the truth table within the cap, the statistics and the analysis.</summary>
public sealed class RuleReportTests
{
    /// <summary>The report of the fixture rule equals the checked-in golden file, byte for byte after <see cref="OutputWriter"/>.</summary>
    [Fact]
    public void Format_FixtureRule_EqualsTheGoldenFile_Test()
    {
        LoadedRule rule = RuleLoader.LoadFile(Fixtures.Path("export.rule")).Value!;

        ToolResult<string> report = RuleReport.Format(rule);

        Assert.True(report.Succeeded);
        Assert.Equal(Fixtures.Read("report/export.report.md"), OutputWriter.Normalize(report.Value));
    }

    /// <summary>A rule over the truth-table cap gets a note in place of the table, and the rest of the report.</summary>
    [Fact]
    public void Format_RuleOverTheCap_HasANoteInPlaceOfTheTable_Test()
    {
        LoadedRule rule = RuleLoader.LoadString("a AND b AND c AND d AND e AND g AND h AND i AND j", "large.rule").Value!;

        ToolResult<string> report = RuleReport.Format(rule);

        Assert.True(report.Succeeded);
        Assert.Contains(
            "The truth table is left out. The rule has 9 distinct terms and the table would have 19683 rows, more than the cap of 8 terms allows.",
            report.Value,
            StringComparison.Ordinal
        );
        Assert.Contains("## Statistics", report.Value, StringComparison.Ordinal);
        Assert.Empty(report.Diagnostics);
    }

    /// <summary>With a larger table allowed, the report has the table and the <c>TRE0034</c> warning.</summary>
    [Fact]
    public void Format_RuleOverTheCapWithLargerTableAllowed_HasTheTableAndAWarning_Test()
    {
        LoadedRule rule = RuleLoader.LoadString("a AND b AND c AND d AND e AND g AND h AND i AND j").Value!;

        ToolResult<string> report = RuleReport.Format(rule, new ExportOptions { AllowLargerTruthTable = true });

        Assert.True(report.Succeeded);
        Assert.Contains("| a | b | c |", report.Value, StringComparison.Ordinal);
        Assert.Contains(report.Diagnostics, d => d.Code == DiagnosticCodes.TruthTableCapRaised);
    }

    /// <summary>The analysis section lists the findings of the analysis level with their code and path.</summary>
    [Fact]
    public void Format_RuleWithATautology_ListsTheFindingInTheAnalysis_Test()
    {
        LoadedRule rule = RuleLoader.LoadString("isActive AND (isLocked OR True)").Value!;

        ToolResult<string> report = RuleReport.Format(rule);

        Assert.True(report.Succeeded);
        string analysis = report.Value[report.Value.IndexOf("## Analysis", StringComparison.Ordinal)..];
        Assert.Contains($"`{DiagnosticCodes.StructuralTautology}`", analysis, StringComparison.Ordinal);
    }

    /// <summary>A rule that does not compile has no report; the result has the compile diagnostics.</summary>
    [Fact]
    public void Format_RuleThatDoesNotCompile_GivesTheCompileDiagnostics_Test()
    {
        ToolResult<string> report = RuleReport.Format(RuleLoader.LoadString("isActive AND (").Value!);

        Assert.False(report.Succeeded);
        Assert.Contains(report.Diagnostics, d => d.Code == DiagnosticCodes.SyntaxError);
    }
}
