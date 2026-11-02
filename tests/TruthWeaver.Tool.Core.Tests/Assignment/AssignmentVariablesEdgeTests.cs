namespace TruthWeaver.Tool.Core.Tests.Assignment;

using System.Text.Json.Nodes;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;
using DataSourceSet = TruthWeaver.Abstractions.DataSources;

/// <summary>The failure modes of the data document and the YAML block writer.</summary>
public sealed class AssignmentVariablesEdgeTests
{
    /// <summary>Malformed JSON is an error with a line number, and the message does not repeat the document.</summary>
    [Fact]
    public void Read_MalformedJson_ReportsTheLineWithoutTheContent_Test()
    {
        ToolResult<JsonObject> result = AssignmentVariables.Read("{\n\"secret\": \n");

        Assert.False(result.Succeeded);
        Assert.Equal(ToolDiagnosticCodes.MalformedDocument, result.Diagnostics[0].Code);
        Assert.Contains("line", result.Diagnostics[0].Message, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", result.Diagnostics[0].Message, StringComparison.Ordinal);
    }

    /// <summary>Malformed YAML is an error with a line and column.</summary>
    [Fact]
    public void Read_MalformedYaml_ReportsLineAndColumn_Test()
    {
        ToolResult<JsonObject> result = AssignmentVariables.Read("a: 'unterminated\n");

        Assert.False(result.Succeeded);
        Assert.Contains("YAML data is not well-formed", result.Diagnostics[0].Message, StringComparison.Ordinal);
    }

    /// <summary>YAML nested deeper than 64 levels is an error and not a stack overflow.</summary>
    [Fact]
    public void Read_YamlNestedTooDeep_IsAnError_Test()
    {
        string text = "a: " + new string('[', 100) + new string(']', 100) + "\n";

        ToolResult<JsonObject> result = AssignmentVariables.Read(text);

        Assert.False(result.Succeeded);
        Assert.Contains("too deep", result.Diagnostics[0].Message, StringComparison.Ordinal);
    }

    /// <summary>A YAML document of only comments is an object with no source.</summary>
    [Fact]
    public void Read_YamlWithOnlyComments_IsAnEmptyObject_Test()
    {
        ToolResult<JsonObject> result = AssignmentVariables.Read("# nothing here\n");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    /// <summary>Quoted YAML scalars stay strings, even when they look like a number, a Boolean or null.</summary>
    [Fact]
    public void Read_YamlQuotedScalars_StayStrings_Test()
    {
        ToolResult<JsonObject> result = AssignmentVariables.Read("s:\n  a: \"12\"\n  b: 'true'\n  c: \"null\"\n  d: 1.5\n");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal("""{"s":{"a":"12","b":"true","c":"null","d":1.5}}""", result.Value.ToJsonString());
    }

    /// <summary>Parse makes one data source per source name.</summary>
    [Fact]
    public void Parse_TwoSources_MakesOneDataSourceForEach_Test()
    {
        ToolResult<DataSourceSet> result = AssignmentVariables.Parse("""{"user": {"region": "EU"}, "limits": {"max": 3}}""");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
    }

    /// <summary>Parse of a document that is not an object gives the diagnostics and no data sources.</summary>
    [Fact]
    public void Parse_NotAnObject_GivesTheDiagnostics_Test()
    {
        ToolResult<DataSourceSet> result = AssignmentVariables.Parse("[1]");

        Assert.False(result.Succeeded);
        Assert.Null(result.Value);
    }

    /// <summary>Empty collections, null and scalars are written on the line of their key or dash.</summary>
    [Fact]
    public void ToYamlBlock_EmptyCollectionsAndNull_AreWrittenInline_Test()
    {
        JsonNode? value = JsonNode.Parse("""{"a": {}, "b": [], "c": null, "d": [1, {"e": "x"}]}""");

        string block = AssignmentVariables.ToYamlBlock(value, 2);

        Assert.Equal("  \"a\": {}\n  \"b\": []\n  \"c\": null\n  \"d\":\n    - 1\n    -\n      \"e\": \"x\"", block);
    }
}
