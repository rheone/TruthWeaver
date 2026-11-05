namespace TruthWeaver.Tool.Core.Tests.Documents;

using TruthWeaver.Tool.Core.Documents;
using YamlDotNet.RepresentationModel;

/// <summary>Walking a parsed path through a <see cref="YamlNode"/> tree with <see cref="DocumentPath.Locate{TNode}"/>.</summary>
public sealed class YamlNodePathNavigatorTests
{
    private const string Yaml = "rules:\n  admin:\n    - id: 1\n    - id: 2\n";

    /// <summary>A key path finds the mapping value with its holding mapping.</summary>
    [Fact]
    public void Locate_KeyPath_FindsTheValueAndItsParent_Test()
    {
        YamlNode root = Parse();

        PathLocation<YamlNode> location = Path("$.rules.admin").Locate(YamlNodePathNavigator.Instance, root);

        Assert.True(location.Found);
        Assert.IsType<YamlSequenceNode>(location.Node);
        Assert.Same(((YamlMappingNode)root).Children[new YamlScalarNode("rules")], location.Parent);
        Assert.Null(location.Stopped);
    }

    /// <summary>An index path finds the sequence item.</summary>
    [Fact]
    public void Locate_IndexPath_FindsTheSequenceItem_Test()
    {
        PathLocation<YamlNode> location = Path("$.rules.admin[1]").Locate(YamlNodePathNavigator.Instance, Parse());

        Assert.True(location.Found);
        Assert.Equal(1, location.Last!.Position);
        Assert.Equal("2", ((YamlScalarNode)((YamlMappingNode)location.Node)["id"]).Value);
    }

    /// <summary>A step that names no child stops the walk and names the segment where it stopped.</summary>
    [Fact]
    public void Locate_MissingStep_ReportsTheStoppingSegment_Test()
    {
        PathLocation<YamlNode> location = Path("$.rules.user[0]").Locate(YamlNodePathNavigator.Instance, Parse());

        Assert.False(location.Found);
        Assert.Equal("user", location.Stopped!.Name);
        Assert.Equal(1, location.StoppedIndex);
    }

    /// <summary>Replace puts another node in the place of the located sequence item.</summary>
    [Fact]
    public void Replace_LocatedItem_PutsTheNodeInItsParent_Test()
    {
        YamlNode root = Parse();
        PathLocation<YamlNode> location = Path("$.rules.admin[0]").Locate(YamlNodePathNavigator.Instance, root);

        bool replaced = location.Replace(new YamlScalarNode("x"));

        Assert.True(replaced);
        Assert.Equal("x", ((YamlScalarNode)Path("$.rules.admin[0]").Locate(YamlNodePathNavigator.Instance, root).Node!).Value);
    }

    /// <summary>Replace puts another node in the place of the located mapping value.</summary>
    [Fact]
    public void Replace_LocatedKey_PutsTheNodeInItsMapping_Test()
    {
        YamlNode root = Parse();
        PathLocation<YamlNode> location = Path("$.rules.admin").Locate(YamlNodePathNavigator.Instance, root);

        bool replaced = location.Replace(new YamlScalarNode("none"));

        Assert.True(replaced);
        Assert.Equal("none", ((YamlScalarNode)Path("$.rules.admin").Locate(YamlNodePathNavigator.Instance, root).Node!).Value);
    }

    private static DocumentPath Path(string text)
    {
        Assert.True(DocumentPath.TryParse(text, out DocumentPath? path, out _));
        return path;
    }

    private static YamlNode Parse()
    {
        YamlStream stream = [];
        using StringReader reader = new(Yaml);
        stream.Load(reader);
        return stream.Documents[0].RootNode;
    }
}
