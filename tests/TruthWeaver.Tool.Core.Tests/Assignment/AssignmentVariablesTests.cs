namespace TruthWeaver.Tool.Core.Tests.Assignment;

using System.Text.Json.Nodes;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;

public sealed class AssignmentVariablesTests
{
    /// <summary>YAML scalars read as JSON would: numbers, Booleans and null are typed, quoted scalars are strings.</summary>
    [Fact]
    public void Read_Yaml_TypesPlainScalars_Test()
    {
        ToolResult<JsonObject> read = AssignmentVariables.Read(
            "user:\n  age: 42\n  price: 1.5\n  ok: true\n  none: ~\n  code: \"42\"\n"
        );

        Assert.True(read.Succeeded);
        Assert.Equal(
            "{\"user\":{\"age\":42,\"price\":1.5,\"ok\":true,\"none\":null,\"code\":\"42\"}}",
            read.Value.ToJsonString()
        );
    }

    /// <summary>An empty document is an object with no source.</summary>
    [Fact]
    public void Read_EmptyText_IsAnEmptyObject_Test()
    {
        ToolResult<JsonObject> read = AssignmentVariables.Read("  \n");

        Assert.True(read.Succeeded);
        Assert.Empty(read.Value);
    }

    /// <summary>A document that is not an object is an error, and the message does not repeat the content.</summary>
    [Fact]
    public void Read_ArrayDocument_IsAnErrorWithoutTheContent_Test()
    {
        ToolResult<JsonObject> read = AssignmentVariables.Read("[\"secret-value\"]");

        Assert.False(read.Succeeded);
        Diagnostic error = Assert.Single(read.Diagnostics);
        Assert.Equal(ToolDiagnosticCodes.MalformedDocument, error.Code);
        Assert.DoesNotContain("secret-value", error.Message, StringComparison.Ordinal);
    }

    /// <summary>A duplicate key in YAML is an error, not an exception.</summary>
    [Fact]
    public void Read_YamlDuplicateKey_IsAnError_Test()
    {
        ToolResult<JsonObject> read = AssignmentVariables.Read("user: {a: 1}\nuser: {a: 2}\n");

        Assert.False(read.Succeeded);
    }

    /// <summary>The block YAML writer gives text that reads back as the same data.</summary>
    [Fact]
    public void ToYamlBlock_NestedData_ReadsBackAsTheSameData_Test()
    {
        JsonObject data = JsonNode
            .Parse(
                "{\"user\":{\"region\":\"EU\",\"tags\":[\"a\",\"b: c\"],\"n\":3,\"empty\":{},\"none\":null,\"list\":[{\"x\":1}]}}"
            )!
            .AsObject();

        string yaml = AssignmentVariables.ToYamlBlock(data, 0);
        ToolResult<JsonObject> read = AssignmentVariables.Read(yaml);

        Assert.True(read.Succeeded, yaml);
        Assert.Equal(data.ToJsonString(), read.Value.ToJsonString());
    }
}
