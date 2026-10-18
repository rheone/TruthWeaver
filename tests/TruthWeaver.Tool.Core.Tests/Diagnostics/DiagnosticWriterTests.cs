namespace TruthWeaver.Tool.Core.Tests.Diagnostics;

using System.Text.Json;
using System.Text.Json.Nodes;
using global::Json.Schema;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Core.Tests.TestSupport;

/// <summary>The diagnostics writers give text with spans, JSON and SARIF 2.1.0 that validates against its schema.</summary>
public sealed class DiagnosticWriterTests
{
    private static readonly JsonSchema SarifSchema = JsonSchema.FromFile(
        Path.Combine(AppContext.BaseDirectory, "TestSupport", "sarif-schema-2.1.0.json")
    );

    /// <summary>The text output names the input, gives the line and column, and underlines the span.</summary>
    [Fact]
    public void Format_Text_GivesInputLineColumnAndUnderline_Test()
    {
        DiagnosticDocument document = Compiled("rules/a.rule", "a ANDD b");

        string text = DiagnosticWriter.Format([document], DiagnosticOutputFormat.Text);

        string[] lines = text.Split('\n');
        Assert.StartsWith("rules/a.rule: TRE0001 error at line 1, column 3: ", lines[0], StringComparison.Ordinal);
        Assert.Equal("  a ANDD b", lines[1]);
        Assert.Equal("    ^^^^", lines[2]);
    }

    /// <summary>The JSON output has the code, its name from the table, the severity, the span and the line and column.</summary>
    [Fact]
    public void Format_Json_GivesCodeNameSeveritySpanAndLocation_Test()
    {
        DiagnosticDocument document = Compiled("rules/a.rule", "a ANDD b");

        JsonNode output = JsonNode.Parse(DiagnosticWriter.Format([document], DiagnosticOutputFormat.Json))!;

        JsonNode diagnostic = output["diagnostics"]![0]!;
        Assert.Equal("rules/a.rule", (string?)diagnostic["origin"]);
        Assert.Equal("TRE0001", (string?)diagnostic["code"]);
        Assert.Equal("SyntaxError", (string?)diagnostic["name"]);
        Assert.Equal("error", (string?)diagnostic["severity"]);
        Assert.Equal(2, (int?)diagnostic["span"]!["start"]);
        Assert.Equal(4, (int?)diagnostic["span"]!["length"]);
        Assert.Equal(
            (1, 3, 1, 7),
            ((int)diagnostic["line"]!, (int)diagnostic["column"]!, (int)diagnostic["endLine"]!, (int)diagnostic["endColumn"]!)
        );
    }

    /// <summary>A SARIF log with rule, tool and whole-input diagnostics validates against the SARIF 2.1.0 schema.</summary>
    [Fact]
    public void Format_Sarif_ValidatesAgainstTheSarifSchema_Test()
    {
        DiagnosticDocument rule = Compiled("rules/a b.rule", "a ANDD b\nOR c");
        DiagnosticDocument embedded = new(
            "policies.yaml",
            "op: andd\n",
            "$.rules.publish",
            new EquatableArray<Diagnostic>([
                Diagnostic.Error(ToolDiagnosticCodes.TreeSchemaViolation, "bad", new SourceSpan(4, 4), path: "$.op"),
            ])
        );
        DiagnosticDocument input = new(
            "-",
            null,
            null,
            new EquatableArray<Diagnostic>([
                Diagnostic.Error(ToolDiagnosticCodes.InputUnreadable, "cannot read", SourceSpan.None),
            ])
        );

        string sarif = DiagnosticWriter.Format([rule, embedded, input], DiagnosticOutputFormat.Sarif);

        using JsonDocument document = JsonDocument.Parse(sarif);
        EvaluationResults results = SarifSchema.Evaluate(
            document.RootElement,
            new EvaluationOptions { OutputFormat = OutputFormat.List, RequireFormatValidation = true }
        );
        Assert.True(
            results.IsValid,
            string.Join('\n', (results.Details ?? []).Where(d => !d.IsValid).Select(d => d.ToString()))
        );
    }

    /// <summary>The SARIF rules are the rows of the code table, and each result names its row by id and index.</summary>
    [Fact]
    public void Format_Sarif_RulesComeFromTheCodeTable_Test()
    {
        DiagnosticDocument document = Compiled("a.rule", "a ANDD b");

        JsonNode run = JsonNode.Parse(DiagnosticWriter.Format([document], DiagnosticOutputFormat.Sarif))!["runs"]![0]!;

        JsonArray rules = run["tool"]!["driver"]!["rules"]!.AsArray();
        Assert.Equal(DiagnosticCodeTable.All.Select(r => r.Code), rules.Select(r => (string)r!["id"]!));
        Assert.Contains(rules, r => (string?)r!["id"] == ToolDiagnosticCodes.InputUnreadable);
        JsonNode result = run["results"]![0]!;
        Assert.Equal("TRE0001", (string?)result["ruleId"]);
        Assert.Equal("TRE0001", (string?)rules[(int)result["ruleIndex"]!]!["id"]);
        Assert.Equal("error", (string?)result["level"]);
    }

    /// <summary>A result of a whole-input rule has the file and a region from the span.</summary>
    [Fact]
    public void Format_Sarif_WholeInputRuleHasRegion_Test()
    {
        DiagnosticDocument document = Compiled("rules/a b.rule", "x AND\ny AND )");

        JsonNode location = SarifResult(document)["locations"]![0]!;

        Assert.Equal("rules/a%20b.rule", (string?)location["physicalLocation"]!["artifactLocation"]!["uri"]);
        JsonNode region = location["physicalLocation"]!["region"]!;
        Assert.Equal(
            (2, 7, 2, 8),
            ((int)region["startLine"]!, (int)region["startColumn"]!, (int)region["endLine"]!, (int)region["endColumn"]!)
        );
        Assert.Equal(12, (int?)region["charOffset"]);
    }

    /// <summary>A result of an embedded rule has the file, no region, and the full tree path as a logical location.</summary>
    [Fact]
    public void Format_Sarif_EmbeddedRuleHasLogicalLocation_Test()
    {
        DiagnosticDocument document = new(
            "policies.yaml",
            "op: andd\n",
            "$.rules.publish",
            new EquatableArray<Diagnostic>([
                Diagnostic.Error(ToolDiagnosticCodes.TreeSchemaViolation, "bad", new SourceSpan(4, 4), path: "$.op"),
            ])
        );

        JsonNode location = SarifResult(document)["locations"]![0]!;

        Assert.Null(location["physicalLocation"]!["region"]);
        Assert.Equal("$.rules.publish.op", (string?)location["logicalLocations"]![0]!["fullyQualifiedName"]);
    }

    /// <summary>An absolute input path is a file URI.</summary>
    [Fact]
    public void Format_Sarif_AbsolutePathIsFileUri_Test()
    {
        string path = Fixtures.Path("access.rule");
        DiagnosticDocument document = Compiled(path, "a ANDD b");

        JsonNode location = SarifResult(document)["locations"]![0]!;

        Assert.Equal(new Uri(path).AbsoluteUri, (string?)location["physicalLocation"]!["artifactLocation"]!["uri"]);
    }

    /// <summary>The same diagnostics give the same bytes each time, with line feeds only.</summary>
    /// <param name="format">The output format.</param>
    [Theory]
    [InlineData(DiagnosticOutputFormat.Text)]
    [InlineData(DiagnosticOutputFormat.Json)]
    [InlineData(DiagnosticOutputFormat.Sarif)]
    public void Format_SameInput_IsDeterministicWithLineFeeds_Test(DiagnosticOutputFormat format)
    {
        DiagnosticDocument document = Compiled("a.rule", "a ANDD b\r\nOR c");

        string first = DiagnosticWriter.Format([document], format);

        Assert.Equal(first, DiagnosticWriter.Format([document], format));
        Assert.DoesNotContain('\r', first);
    }

    /// <summary>The writer to a text writer gives the same text as the string form.</summary>
    [Fact]
    public void Write_TextWriter_GivesTheFormattedText_Test()
    {
        DiagnosticDocument document = Compiled("a.rule", "a ANDD b");
        using StringWriter writer = new();

        DiagnosticWriter.Write(writer, [document], DiagnosticOutputFormat.Json);

        Assert.Equal(DiagnosticWriter.Format([document], DiagnosticOutputFormat.Json), writer.ToString());
    }

    private static JsonNode SarifResult(DiagnosticDocument document)
    {
        return JsonNode.Parse(DiagnosticWriter.Format([document], DiagnosticOutputFormat.Sarif))!["runs"]![0]!["results"]![0]!;
    }

    private static DiagnosticDocument Compiled(string origin, string text)
    {
        LoadedRule rule = RuleLoader.LoadString(text, origin, new RuleLoadOptions { Format = RuleFormat.Text }).Value!;
        return DiagnosticDocument.For(rule, RuleConverter.Compile(rule).Diagnostics);
    }
}
