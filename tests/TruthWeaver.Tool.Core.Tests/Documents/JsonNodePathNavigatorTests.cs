namespace TruthWeaver.Tool.Core.Tests.Documents;

using System.Text.Json.Nodes;
using TruthWeaver.Tool.Core.Documents;

/// <summary>Walking a parsed path through a <see cref="JsonNode"/> tree with <see cref="DocumentPath.Locate{TNode}"/>.</summary>
public sealed class JsonNodePathNavigatorTests
{
    private const string Json = """{"rules":{"admin":[{"id":1},{"id":2}]}}""";

    /// <summary>A key path finds the member with its holding object.</summary>
    [Fact]
    public void Locate_KeyPath_FindsTheMemberAndItsParent_Test()
    {
        JsonNode root = Parse();

        PathLocation<JsonNode> location = Path("$.rules.admin").Locate(JsonNodePathNavigator.Instance, root);

        Assert.True(location.Found);
        Assert.Same(root["rules"]!["admin"], location.Node);
        Assert.Same(root["rules"], location.Parent);
        Assert.Null(location.Stopped);
    }

    /// <summary>An index path finds the array item.</summary>
    [Fact]
    public void Locate_IndexPath_FindsTheArrayItem_Test()
    {
        JsonNode root = Parse();

        PathLocation<JsonNode> location = Path("$.rules.admin[1]").Locate(JsonNodePathNavigator.Instance, root);

        Assert.Same(root["rules"]!["admin"]![1], location.Node);
        Assert.Equal(1, location.Last!.Position);
    }

    /// <summary>A step that names no child stops the walk and names the segment where it stopped.</summary>
    [Fact]
    public void Locate_MissingStep_ReportsTheStoppingSegment_Test()
    {
        PathLocation<JsonNode> location = Path("$.rules.user[0]").Locate(JsonNodePathNavigator.Instance, Parse());

        Assert.False(location.Found);
        Assert.Equal("user", location.Stopped!.Name);
    }

    /// <summary>An index past the end of an array stops the walk at the index segment.</summary>
    [Fact]
    public void Locate_IndexPastTheEnd_ReportsTheIndexSegment_Test()
    {
        PathLocation<JsonNode> location = Path("$.rules.admin[2]").Locate(JsonNodePathNavigator.Instance, Parse());

        Assert.False(location.Found);
        Assert.Null(location.Stopped!.Name);
        Assert.Equal(2, location.Stopped.Position);
    }

    /// <summary>Replace puts another node in the place of the located node, in its parent.</summary>
    [Fact]
    public void Replace_LocatedItem_PutsTheNodeInItsParent_Test()
    {
        JsonNode root = Parse();
        PathLocation<JsonNode> location = Path("$.rules.admin[0]").Locate(JsonNodePathNavigator.Instance, root);

        bool replaced = location.Replace(new JsonObject { ["id"] = 9 });

        Assert.True(replaced);
        Assert.Equal("""{"rules":{"admin":[{"id":9},{"id":2}]}}""", root.ToJsonString());
    }

    /// <summary>The root has no parent, so replacing it is left to the caller.</summary>
    [Fact]
    public void Replace_Root_ReturnsFalse_Test()
    {
        PathLocation<JsonNode> location = Path("$").Locate(JsonNodePathNavigator.Instance, Parse());

        Assert.True(location.Found);
        Assert.False(location.Replace(new JsonObject()));
    }

    private static DocumentPath Path(string text)
    {
        Assert.True(DocumentPath.TryParse(text, out DocumentPath? path, out _));
        return path;
    }

    private static JsonNode Parse()
    {
        return JsonNode.Parse(Json)!;
    }
}
