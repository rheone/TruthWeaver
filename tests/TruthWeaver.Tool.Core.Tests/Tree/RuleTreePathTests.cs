namespace TruthWeaver.Tool.Core.Tests.Tree;

using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Tree;

/// <summary>Finding a node of a JSON tree by its path, and the path arithmetic that edits use.</summary>
public sealed class RuleTreePathTests
{
    private const string Tree = """{"op":"and","operands":[{"predicate":"a"},{"op":"not","operands":[{"predicate":"b"}]}]}""";

    /// <summary>A path of operand steps finds the node, the operands array that holds it and its index.</summary>
    [Fact]
    public void TryLocate_NestedOperand_FindsNodeParentAndIndex_Test()
    {
        JsonObject root = Parse(Tree);

        bool found = RuleTree.TryLocate(
            root,
            "$.operands[1].operands[0]",
            out JsonObject? node,
            out JsonArray? parent,
            out int index
        );

        Assert.True(found);
        Assert.NotNull(node);
        Assert.Equal("b", node["predicate"]?.GetValue<string>());
        Assert.NotNull(parent);
        Assert.Single(parent);
        Assert.Equal(0, index);
    }

    /// <summary>A path that leaves the tree, is not closed, is negative or does not start at the root finds nothing.</summary>
    [Theory]
    [InlineData("$.operands[2]")]
    [InlineData("$.operands[0")]
    [InlineData("$.operands[-1]")]
    [InlineData("$.operands[0].operands[0]")]
    [InlineData("operands[0]")]
    [InlineData("$.rules")]
    public void TryLocate_PathThatNamesNoNode_ReturnsFalse_Test(string path)
    {
        JsonObject root = Parse(Tree);

        bool found = RuleTree.TryLocate(root, path, out JsonObject? node, out _, out _);

        Assert.False(found);
        Assert.Null(node);
    }

    /// <summary>The root path finds the root, with no parent and the index -1.</summary>
    [Fact]
    public void TryLocate_RootPath_FindsTheRootWithoutAParent_Test()
    {
        JsonObject root = Parse(Tree);

        bool found = RuleTree.TryLocate(root, "$", out JsonObject? node, out JsonArray? parent, out int index);

        Assert.True(found);
        Assert.Same(root, node);
        Assert.Null(parent);
        Assert.Equal(-1, index);
    }

    /// <summary>A key path with a quoted key that holds a quote finds the object the path names.</summary>
    [Fact]
    public void TryLocatePath_QuotedKeyWithEscapedQuote_FindsTheNode_Test()
    {
        JsonObject root = Parse("""{"definitions":{"it's":{"body":{"const":true}}}}""");

        bool found = RuleTree.TryLocatePath(root, "$.definitions['it\\'s'].body", out JsonObject? node, out _);

        Assert.True(found);
        Assert.Same(root["definitions"]?["it's"]?["body"], node);
    }

    /// <summary>The put action of a quoted-key path puts another object in the place of the node.</summary>
    [Fact]
    public void TryLocatePath_QuotedKeyWithEscapedQuote_PutReplacesTheNodeInPlace_Test()
    {
        JsonObject root = Parse("""{"definitions":{"it's":{"body":{"const":true}}}}""");
        RuleTree.TryLocatePath(root, "$.definitions['it\\'s'].body", out _, out Action<JsonObject>? put);
        Assert.NotNull(put);

        put(new JsonObject { ["const"] = false });

        Assert.False(Assert.IsType<JsonObject>(root["definitions"]?["it's"]?["body"])["const"]?.GetValue<bool>());
    }

    /// <summary>A path that ends at a value that is not an object finds nothing.</summary>
    [Fact]
    public void TryLocatePath_PathToAScalar_ReturnsFalse_Test()
    {
        JsonObject root = Parse("""{"a":{"b":1}}""");

        bool found = RuleTree.TryLocatePath(root, "$.a.b", out JsonObject? node, out Action<JsonObject>? put);

        Assert.False(found);
        Assert.Null(node);
        Assert.Null(put);
    }

    /// <summary>The path of an operand extends the path of its node with an operand step.</summary>
    [Fact]
    public void OperandPath_NodeAndIndex_AppendsTheOperandStep_Test()
    {
        Assert.Equal("$.operands[1].operands[0]", RuleTree.OperandPath("$.operands[1]", 0));
    }

    /// <summary>The parent path of an operand path is the path of the node that holds it.</summary>
    [Fact]
    public void ParentPath_OperandPath_GivesTheHoldingNode_Test()
    {
        Assert.Equal("$.operands[1]", RuleTree.ParentPath("$.operands[1].operands[0]"));
    }

    /// <summary>The root has no parent path.</summary>
    [Fact]
    public void ParentPath_Root_ReturnsNull_Test()
    {
        Assert.Null(RuleTree.ParentPath("$"));
    }

    /// <summary>Replacing the members keeps the same instance, empties the replacement and drops old members.</summary>
    [Fact]
    public void ReplaceMembers_NewMembers_KeepsTheTargetInstanceAndDropsTheOldMembers_Test()
    {
        JsonObject target = new() { ["old"] = 1 };
        JsonObject replacement = new() { ["op"] = "not", ["operands"] = new JsonArray() };

        RuleTree.ReplaceMembers(target, replacement);

        Assert.Equal("""{"op":"not","operands":[]}""", target.ToJsonString());
        Assert.Empty(replacement);
    }

    /// <summary>The text of the whole rule, at the path <c>$</c>, is the printed rule.</summary>
    [Fact]
    public void TextOf_RootPath_GivesTheWholeRuleText_Test()
    {
        RuleTree tree = RuleTree.Of(RuleDocument.Create("a AND b").Rule!);

        ToolResult<string> text = tree.TextOf("$");

        Assert.True(text.Succeeded);
        Assert.Equal("a AND b", text.Value);
    }

    /// <summary>The label of a threshold operator names its count, so the tree shows <c>atLeast k=2</c>.</summary>
    [Fact]
    public void Of_AtLeastOperator_LabelsTheOperatorWithItsCount_Test()
    {
        const string Json = """{"op": "atLeast", "k": 2, "operands": [{"predicate": "a"}, {"predicate": "b"}]}""";
        RuleTree tree = RuleTree.Of(RuleDocument.Create(Json, RuleFormat.Json).Rule!);

        Assert.Equal("atLeast k=2", tree.Find("$")!.Label);
    }

    /// <summary>A path that does not start at the root, or that leaves the tree, names no editable node.</summary>
    /// <param name="path">The path.</param>
    [Theory]
    [InlineData("operands[0]")]
    [InlineData("$.operands[9]")]
    [InlineData("$.operands[0].operands[0]")]
    [InlineData("$.nothing")]
    public void TryLocatePath_PathThatNamesNoNode_ReturnsFalse_Test(string path)
    {
        JsonObject root = (JsonObject)JsonNode.Parse(Tree)!;

        bool found = RuleTree.TryLocatePath(root, path, out JsonObject? node, out Action<JsonObject>? put);

        Assert.False(found);
        Assert.Null(node);
        Assert.Null(put);
    }

    private static JsonObject Parse(string json)
    {
        return Assert.IsType<JsonObject>(JsonNode.Parse(json));
    }
}
