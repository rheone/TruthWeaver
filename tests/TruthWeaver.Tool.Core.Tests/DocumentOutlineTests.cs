namespace TruthWeaver.Tool.Core.Tests;

using TruthWeaver.Compilation;

public sealed class DocumentOutlineTests
{
    private const string Yaml = """
        service: editor
        rules:
          publish:
            op: and
            operands:
              - predicate: isActive
              - predicate: isAdmin
          review: 'isActive AND isReviewer'
          list:
            - {op: not, operands: [{predicate: isBanned}]}
            - 'isGuest'
        count: 3
        enabled: true

        """;

    /// <summary>The outline lists containers, rules and strings in document order, with their normalized paths.</summary>
    [Fact]
    public void Browse_YamlDocument_ListsRulesAndStringsInDocumentOrder_Test()
    {
        ToolResult<IReadOnlyList<DocumentNode>> result = DocumentOutline.Browse(Yaml, RuleFormat.Yaml);

        Assert.True(result.Succeeded);
        Assert.Equal(
            [
                "$",
                "$.service",
                "$.rules",
                "$.rules.publish",
                "$.rules.review",
                "$.rules.list",
                "$.rules.list[0]",
                "$.rules.list[1]",
            ],
            result.Value.Select(n => n.Path)
        );
    }

    /// <summary>A rule in the tree format is one entry, and the nodes inside it are not listed.</summary>
    [Fact]
    public void Browse_YamlDocument_DoesNotListTheNodesInsideARule_Test()
    {
        IReadOnlyList<DocumentNode> nodes = DocumentOutline.Browse(Yaml, RuleFormat.Yaml).Value!;

        DocumentNode publish = Assert.Single(nodes, n => n.Path == "$.rules.publish");
        Assert.Equal(DocumentNodeKind.Rule, publish.Kind);
        Assert.Equal("rule (op: and)", publish.Label);
        Assert.DoesNotContain(nodes, n => n.Path.StartsWith("$.rules.publish.", StringComparison.Ordinal));
    }

    /// <summary>A JSON document is listed the same way, with strings as text entries.</summary>
    [Fact]
    public void Browse_JsonDocument_ListsStringsAsText_Test()
    {
        const string json = """{ "a": { "b": "x AND y", "n": 1 }, "r": { "predicate": "p" } }""";

        IReadOnlyList<DocumentNode> nodes = DocumentOutline.Browse(json, RuleFormat.Json).Value!;

        Assert.Equal(["$", "$.a", "$.a.b", "$.r"], nodes.Select(n => n.Path));
        Assert.Equal(DocumentNodeKind.Text, nodes[2].Kind);
        Assert.Equal(DocumentNodeKind.Rule, nodes[3].Kind);
    }

    /// <summary>A document that is not well formed gives the malformed-document error, not an exception.</summary>
    [Fact]
    public void Browse_MalformedJson_ReturnsAnError_Test()
    {
        ToolResult<IReadOnlyList<DocumentNode>> result = DocumentOutline.Browse("{ \"a\": ", RuleFormat.Json);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == ToolDiagnosticCodes.MalformedDocument);
    }

    /// <summary>Rule text is not a document, so browsing it is refused.</summary>
    [Fact]
    public void Browse_RuleTextFormat_Throws_Test()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DocumentOutline.Browse("a AND b", RuleFormat.Text));
    }

    /// <summary>A pattern with a wildcard key matches every entry at that place.</summary>
    [Fact]
    public void Match_WildcardKey_MatchesEveryEntryAtThatPlace_Test()
    {
        IReadOnlyList<DocumentNode> nodes = DocumentOutline.Browse(Yaml, RuleFormat.Yaml).Value!;

        ToolResult<IReadOnlyList<DocumentNode>> result = DocumentOutline.Match(nodes, "$.rules.*");

        Assert.Equal(["$.rules.publish", "$.rules.review", "$.rules.list"], result.Value!.Select(n => n.Path));
    }

    /// <summary>A pattern with a wildcard index matches every item of the array.</summary>
    [Fact]
    public void Match_WildcardIndex_MatchesEveryItem_Test()
    {
        IReadOnlyList<DocumentNode> nodes = DocumentOutline.Browse(Yaml, RuleFormat.Yaml).Value!;

        ToolResult<IReadOnlyList<DocumentNode>> result = DocumentOutline.Match(nodes, "rules.list[*]");

        Assert.Equal(["$.rules.list[0]", "$.rules.list[1]"], result.Value!.Select(n => n.Path));
    }

    /// <summary>A pattern with no wildcard matches the one entry it names, or none.</summary>
    [Theory]
    [InlineData("$.rules.review", 1)]
    [InlineData("rules['review']", 1)]
    [InlineData("$.rules.nothing", 0)]
    public void Match_PlainPath_MatchesOneOrNone_Test(string pattern, int count)
    {
        IReadOnlyList<DocumentNode> nodes = DocumentOutline.Browse(Yaml, RuleFormat.Yaml).Value!;

        Assert.Equal(count, DocumentOutline.Match(nodes, pattern).Value!.Count);
    }

    /// <summary>A pattern that is not a path gives the invalid-path error.</summary>
    [Fact]
    public void Match_InvalidPattern_ReturnsTheInvalidPathError_Test()
    {
        IReadOnlyList<DocumentNode> nodes = DocumentOutline.Browse(Yaml, RuleFormat.Yaml).Value!;

        ToolResult<IReadOnlyList<DocumentNode>> result = DocumentOutline.Match(nodes, "rules..x");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == ToolDiagnosticCodes.InvalidPath);
    }
}
