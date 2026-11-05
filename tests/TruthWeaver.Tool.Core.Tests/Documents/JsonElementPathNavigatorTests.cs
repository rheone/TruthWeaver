namespace TruthWeaver.Tool.Core.Tests.Documents;

using System.Text.Json;
using TruthWeaver.Tool.Core.Documents;

/// <summary>Walking a parsed path through a <see cref="JsonElement"/> tree with <see cref="DocumentPath.Locate{TNode}"/>.</summary>
public sealed class JsonElementPathNavigatorTests
{
    private const string Json = """{"rules":{"admin":[{"id":1},{"id":2}]}}""";

    /// <summary>A key path finds the member with its holding object.</summary>
    [Fact]
    public void Locate_KeyPath_FindsTheMemberAndItsParent_Test()
    {
        using JsonDocument document = JsonDocument.Parse(Json);

        PathLocation<JsonElementNode> location = Locate(document, "$.rules.admin");

        Assert.True(location.Found);
        Assert.Equal(JsonValueKind.Array, location.Node.Element.ValueKind);
        Assert.Equal(JsonValueKind.Object, location.Parent!.Element.ValueKind);
        Assert.Null(location.Stopped);
    }

    /// <summary>An index path finds the array item.</summary>
    [Fact]
    public void Locate_IndexPath_FindsTheArrayItem_Test()
    {
        using JsonDocument document = JsonDocument.Parse(Json);

        PathLocation<JsonElementNode> location = Locate(document, "$.rules.admin[1]");

        Assert.Equal(2, location.Node!.Element.GetProperty("id").GetInt32());
    }

    /// <summary>A step that names no child stops the walk and names the segment where it stopped.</summary>
    [Fact]
    public void Locate_MissingStep_ReportsTheStoppingSegment_Test()
    {
        using JsonDocument document = JsonDocument.Parse(Json);

        PathLocation<JsonElementNode> location = Locate(document, "$.rules.admin[2]");

        Assert.False(location.Found);
        Assert.Equal(2, location.Stopped!.Position);
        Assert.Equal(2, location.StoppedIndex);
    }

    /// <summary>A parsed <see cref="JsonElement"/> is read-only, so Replace is refused.</summary>
    [Fact]
    public void Replace_LocatedItem_IsRefused_Test()
    {
        using JsonDocument document = JsonDocument.Parse(Json);
        PathLocation<JsonElementNode> location = Locate(document, "$.rules.admin[0]");

        Assert.False(location.Replace(new JsonElementNode(document.RootElement)));
    }

    private static PathLocation<JsonElementNode> Locate(JsonDocument document, string text)
    {
        Assert.True(DocumentPath.TryParse(text, out DocumentPath? path, out _));
        return path.Locate(JsonElementPathNavigator.Instance, new JsonElementNode(document.RootElement));
    }
}
