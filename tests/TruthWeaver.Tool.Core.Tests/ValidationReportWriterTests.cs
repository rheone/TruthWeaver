namespace TruthWeaver.Tool.Core.Tests;

using System.Text.Json.Nodes;
using TruthWeaver.Compilation;

/// <summary>The validation report writer gives the level outcomes, the statistics and the diagnostics.</summary>
public sealed class ValidationReportWriterTests
{
    /// <summary>The JSON report has each level with its status, the statistics and the diagnostics.</summary>
    [Fact]
    public void Format_Json_GivesLevelsStatisticsAndDiagnostics_Test()
    {
        ValidationReport report = Validate("NOT NOT isActive");

        JsonNode output = JsonNode.Parse(ValidationReportWriter.Format(report, DiagnosticOutputFormat.Json))!;

        Assert.Equal("text", (string?)output["format"]);
        JsonArray levels = output["levels"]!.AsArray();
        Assert.Equal(
            ["compile", "treeSchema", "limits", "predicateSchema", "analysis", "conversionCheck"],
            levels.Select(l => (string)l!["level"]!)
        );
        Assert.Equal("skipped", (string?)levels[1]!["status"]);
        Assert.Equal(1, (int?)levels[4]!["diagnostics"]);
        JsonNode statistics = output["statistics"]!;
        Assert.Equal(3, (int?)statistics["nodeCount"]);
        Assert.Equal(2, (int?)statistics["operators"]!["not"]);
        Assert.Equal(3, (int?)statistics["truthTableRows"]);
        Assert.Equal("contingent", (string?)statistics["constancy"]);
        Assert.Equal("TRE0023", (string?)output["diagnostics"]![0]!["code"]);
    }

    /// <summary>The text report lists each level and the statistics, then the diagnostics.</summary>
    [Fact]
    public void Format_Text_ListsLevelsStatisticsAndDiagnostics_Test()
    {
        ValidationReport report = Validate("NOT NOT isActive");

        string text = ValidationReportWriter.Format(report, DiagnosticOutputFormat.Text);

        Assert.Contains("  compile: passed\n", text, StringComparison.Ordinal);
        Assert.Contains("  treeSchema: skipped: ", text, StringComparison.Ordinal);
        Assert.Contains("  operators: not 2\n", text, StringComparison.Ordinal);
        Assert.Contains("  truth table: 3 rows, 2 classical rows, within the cap\n", text, StringComparison.Ordinal);
        Assert.Contains("rule: TRE0023 info", text, StringComparison.Ordinal);
    }

    /// <summary>The SARIF report is the SARIF log of the report's diagnostics.</summary>
    [Fact]
    public void Format_Sarif_IsTheDiagnosticsLog_Test()
    {
        ValidationReport report = Validate("NOT NOT isActive");

        string sarif = ValidationReportWriter.Format(report, DiagnosticOutputFormat.Sarif);

        Assert.Equal(
            DiagnosticWriter.Format([DiagnosticDocument.For(report.Rule, report.Diagnostics)], DiagnosticOutputFormat.Sarif),
            sarif
        );
    }

    private static ValidationReport Validate(string text)
    {
        LoadedRule rule = RuleLoader.LoadString(text, "rule", new RuleLoadOptions { Format = RuleFormat.Text }).Value!;
        return RuleValidator.Validate(rule).Value!;
    }
}
