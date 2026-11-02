namespace TruthWeaver.Tool.Core.Tests.Documents;

using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Loading;
using YamlDotNet.Core;

/// <summary>The source spans that the index finds for a JSON or a YAML document.</summary>
public sealed class SourceNodeIndexTests
{
    /// <summary>A JSON path finds the span of the value, and the span cuts the value out of the document.</summary>
    [Fact]
    public void Find_JsonScalar_SpansExactlyTheValue_Test()
    {
        const string Document = """{"rule": {"op": "and", "operands": [{"const": true}]}}""";
        SourceNodeIndex index = SourceNodeIndex.Create(Document, RuleFormat.Json);

        SourceNode? node = index.Find("$.rule.op");

        Assert.NotNull(node);
        Assert.Equal(SourceNodeKind.Scalar, node.Kind);
        Assert.Equal("\"and\"", Document[node.Start..node.End]);
    }

    /// <summary>A path that the document has no value for gives no node.</summary>
    [Fact]
    public void Find_PathWithNoValue_ReturnsNull_Test()
    {
        SourceNodeIndex index = SourceNodeIndex.Create("""{"a": [1]}""", RuleFormat.Json);

        Assert.Null(index.Find("$.a[3]"));
        Assert.Null(index.Find("$.b"));
    }

    /// <summary>A JSON document that is not well formed gives an empty index, not an exception.</summary>
    [Fact]
    public void Create_MalformedJson_GivesAnEmptyIndex_Test()
    {
        SourceNodeIndex index = SourceNodeIndex.Create("{\"a\": ", RuleFormat.Json);

        Assert.Null(index.Find("$"));
    }

    /// <summary>A block mapping in YAML ends with its last value, without the comment and blank lines after it.</summary>
    [Fact]
    public void Find_YamlBlockMapping_EndsAtItsLastValue_Test()
    {
        const string Document = "rules:\n  a:\n    op: and\n    operands: []\n\n# trailing comment\nother: 1\n";
        SourceNodeIndex index = SourceNodeIndex.Create(Document, RuleFormat.Yaml);

        SourceNode? node = index.Find("$.rules.a");

        Assert.NotNull(node);
        Assert.Equal(SourceNodeKind.Mapping, node.Kind);
        Assert.False(node.IsFlow);
        Assert.EndsWith("operands: []", Document[node.Start..node.End], StringComparison.Ordinal);
    }

    /// <summary>A flow mapping in YAML includes its closing bracket in the span.</summary>
    [Fact]
    public void Find_YamlFlowMapping_IncludesTheClosingBracket_Test()
    {
        const string Document = "rule: {op: not, operands: [{const: true}]}\n";
        SourceNodeIndex index = SourceNodeIndex.Create(Document, RuleFormat.Yaml);

        SourceNode? node = index.Find("$.rule");

        Assert.NotNull(node);
        Assert.True(node.IsFlow);
        Assert.Equal("{op: not, operands: [{const: true}]}", Document[node.Start..node.End]);
    }

    /// <summary>An alias is an alias node, and a value under an anchor is marked as shared.</summary>
    [Fact]
    public void Find_YamlAliasAndAnchor_AreMarkedAliasAndShared_Test()
    {
        const string Document = "base: &x\n  op: and\ncopy: *x\n";
        SourceNodeIndex index = SourceNodeIndex.Create(Document, RuleFormat.Yaml);

        SourceNode? anchored = index.Find("$.base");
        SourceNode? alias = index.Find("$.copy");

        Assert.NotNull(anchored);
        Assert.True(anchored.IsShared);
        Assert.NotNull(alias);
        Assert.Equal(SourceNodeKind.Alias, alias.Kind);
    }

    /// <summary>A YAML document with an error keeps what was indexed before the error.</summary>
    [Fact]
    public void Create_YamlWithAnErrorLater_KeepsTheEarlierNodes_Test()
    {
        SourceNodeIndex index = SourceNodeIndex.Create("a: 1\nb: 'unterminated\n", RuleFormat.Yaml);

        SourceNode? first = index.Find("$.a");

        Assert.NotNull(first);
        Assert.Equal(ScalarStyle.Plain, first.Style);
    }
}
