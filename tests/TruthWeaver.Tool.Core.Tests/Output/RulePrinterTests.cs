namespace TruthWeaver.Tool.Core.Tests.Output;

using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Yaml;

/// <summary>The print policy of <see cref="RulePrinter"/>, which is the only place that holds it.</summary>
public sealed class RulePrinterTests
{
    private const string LongRule =
        "alpha_predicate_one and bravo_predicate_two and charlie_predicate_three and delta_predicate_four and echo_predicate_five";

    /// <summary>The canonical rule text is one line.</summary>
    [Fact]
    public void Print_TextCanonical_IsOneLine_Test()
    {
        string printed = RulePrinter.Print(Compile(LongRule), RuleFormat.Text, ToolSettings.Default, pretty: false);

        Assert.DoesNotContain('\n', printed);
    }

    /// <summary>The pretty rule text breaks lines at the pretty width.</summary>
    [Fact]
    public void Print_TextPretty_BreaksAtPrettyWidth_Test()
    {
        string printed = RulePrinter.Print(Compile(LongRule), RuleFormat.Text, ToolSettings.Default, pretty: true);

        string[] lines = printed.Split('\n');
        Assert.True(lines.Length > 1);
        Assert.All(lines, line => Assert.True(line.Length <= RulePrinter.PrettyWidth, line));
    }

    /// <summary>Rule text keeps the notes of the rule.</summary>
    [Fact]
    public void Print_TextWithNote_KeepsTheNote_Test()
    {
        CompiledRule<StubAssignment> rule = Compile("/* both hold */ (a and b)");

        string printed = RulePrinter.Print(rule, RuleFormat.Text, ToolSettings.Default, pretty: false);

        Assert.Equal("both hold", RuleConverter.CreateCompiler().CompileAs(printed, RuleFormat.Text).Rule!.Notes["$"]);
    }

    /// <summary>Rule text uses the operator style of the settings.</summary>
    [Fact]
    public void Print_TextWithSymbolStyle_UsesTheOperatorStyle_Test()
    {
        ToolSettings settings = ToolSettings.Default with { OperatorStyle = OperatorStyle.Symbolic };

        string printed = RulePrinter.Print(Compile("a and b"), RuleFormat.Text, settings, pretty: false);

        Assert.Contains("∧", printed, StringComparison.Ordinal);
    }

    /// <summary>The pretty JSON is indented by two spaces; the canonical JSON is not.</summary>
    [Fact]
    public void Print_JsonPrettyAndCanonical_IndentOnlyThePrettyForm_Test()
    {
        CompiledRule<StubAssignment> rule = Compile("a and b");

        string canonical = RulePrinter.Print(rule, RuleFormat.Json, ToolSettings.Default, pretty: false);
        string pretty = RulePrinter.Print(rule, RuleFormat.Json, ToolSettings.Default, pretty: true);

        Assert.DoesNotContain('\n', canonical);
        Assert.Contains("\n  ", pretty, StringComparison.Ordinal);
        Assert.DoesNotContain('\r', pretty);
    }

    /// <summary>YAML has line feeds only, in both forms.</summary>
    /// <param name="pretty">The form.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Print_Yaml_HasLineFeedsOnly_Test(bool pretty)
    {
        string printed = RulePrinter.Print(Compile("a and b"), RuleFormat.Yaml, ToolSettings.Default, pretty);

        Assert.DoesNotContain('\r', printed);
    }

    /// <summary>A rule with definitions prints as rule text that keeps its declarations and uses.</summary>
    [Fact]
    public void PrintDefinitions_Text_KeepsDeclarationsAndUses_Test()
    {
        JsonObject document = Definitions("let both(x, y) = x and y; both(a, b) or c");

        string? printed = RulePrinter.PrintDefinitions(document, RuleFormat.Text, ToolSettings.Default, pretty: false);

        Assert.NotNull(printed);
        Assert.StartsWith("let both(x, y) = ", printed, StringComparison.Ordinal);
        Assert.Contains("both(a, b)", printed, StringComparison.Ordinal);
    }

    /// <summary>A rule with definitions prints as JSON, indented only in the pretty form, and as YAML with line feeds only.</summary>
    [Fact]
    public void PrintDefinitions_JsonAndYaml_FollowThePolicy_Test()
    {
        JsonObject document = Definitions("let both(x, y) = x and y; both(a, b) or c");

        string? canonical = RulePrinter.PrintDefinitions(document, RuleFormat.Json, ToolSettings.Default, pretty: false);
        string? pretty = RulePrinter.PrintDefinitions(document, RuleFormat.Json, ToolSettings.Default, pretty: true);
        string? yaml = RulePrinter.PrintDefinitions(document, RuleFormat.Yaml, ToolSettings.Default, pretty: false);

        Assert.NotNull(canonical);
        Assert.NotNull(pretty);
        Assert.DoesNotContain('\n', canonical);
        Assert.Contains("\n  ", pretty, StringComparison.Ordinal);
        Assert.NotNull(yaml);
        Assert.DoesNotContain('\r', yaml);
    }

    /// <summary>Without notes the printed JSON and rule text carry none.</summary>
    [Fact]
    public void Print_WithoutNotes_LeavesTheNotesOut_Test()
    {
        CompiledRule<StubAssignment> rule = Compile("/* both hold */ (a and b)");

        string text = RulePrinter.Print(rule, RuleFormat.Text, ToolSettings.Default, pretty: false, includeNotes: false);
        string json = RulePrinter.Print(rule, RuleFormat.Json, ToolSettings.Default, pretty: false, includeNotes: false);

        Assert.DoesNotContain("both hold", text, StringComparison.Ordinal);
        Assert.DoesNotContain("both hold", json, StringComparison.Ordinal);
    }

    /// <summary>A C-style operator setting has no rule text that reads back, so rule text falls back to the word style.</summary>
    [Fact]
    public void Print_TextWithCStyleOperators_FallsBackToTheWordStyle_Test()
    {
        ToolSettings settings = ToolSettings.Default with { OperatorStyle = OperatorStyle.CStyle };

        string printed = RulePrinter.Print(Compile("a and b"), RuleFormat.Text, settings, pretty: false);

        Assert.Equal("a AND b", printed);
    }

    /// <summary>A JSON node and its text indent to the same text.</summary>
    [Fact]
    public void IndentJson_NodeAndText_GiveTheSameText_Test()
    {
        const string json = "{\"a\":[1,2]}";

        Assert.Equal(RulePrinter.IndentJson(json), RulePrinter.IndentJson(JsonNode.Parse(json)!));
    }

    /// <summary>The indented JSON uses two spaces and does not escape characters that JSON allows as they are.</summary>
    [Fact]
    public void IndentJson_NonAsciiText_IsNotEscaped_Test()
    {
        string indented = RulePrinter.IndentJson("{\"a\":\"café <b>\"}");

        Assert.Equal("{\n  \"a\": \"café <b>\"\n}", indented);
    }

    private static CompiledRule<StubAssignment> Compile(string text)
    {
        return RuleConverter.CreateCompiler().CompileAs(text, RuleFormat.Text).Rule!;
    }

    private static JsonObject Definitions(string text)
    {
        return (JsonObject)JsonNode.Parse(YamlRuleDefinitions.ReadAs(text, RuleFormat.Text).PrintJson()!)!;
    }
}
