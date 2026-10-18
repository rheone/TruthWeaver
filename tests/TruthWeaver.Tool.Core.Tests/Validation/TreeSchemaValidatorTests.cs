namespace TruthWeaver.Tool.Core.Tests.Validation;

using System.Text.Json;
using System.Text.Json.Nodes;
using global::Json.Schema;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Diagnostics;
using TruthWeaver.Tool.Core.Validation;

/// <summary>
/// The tree schema check reads the rule tree schema the way a JSON schema validator does, for JSON and YAML rules, and the
/// schema uses only the keywords that the check reads.
/// </summary>
public sealed class TreeSchemaValidatorTests
{
    private static readonly JsonSchema Reference = JsonSchema.FromText(TreeSchemaValidator.SchemaText());

    /// <summary>Gets JSON rules, valid and not valid, that cover each keyword of the schema.</summary>
    public static TheoryData<string> JsonRules =>
        [
            """{"const": true}""",
            """{"const": "Unknown"}""",
            """{"const": "maybe"}""",
            """{"const": 1}""",
            """{"predicate": "a"}""",
            """{"predicate": "a", "note": "A note."}""",
            """{"predicate": "a", "args": {"n": 1, "s": "x", "list": [1, 2], "v": {"from": "user", "query": "$.a"}}}""",
            """{"predicate": "a", "args": {"v": {"from": "user"}}}""",
            """{"predicate": "a", "args": {"v": null}}""",
            """{"predicate": 1}""",
            """{"op": "not", "operands": [{"predicate": "a"}]}""",
            """{"op": "not", "operands": [{"predicate": "a"}, {"predicate": "b"}]}""",
            """{"op": "not", "operands": []}""",
            """{"op": "AND", "operands": [{"predicate": "a"}, {"op": "or", "operands": [{"const": false}]}]}""",
            """{"op": "andd", "operands": []}""",
            """{"op": "atLeast", "k": 1, "operands": [{"predicate": "a"}]}""",
            """{"op": "atLeast", "k": 1.5, "operands": []}""",
            """{"op": "atLeast", "operands": []}""",
            """{"op": "between", "min": 0, "max": 1, "operands": []}""",
            """{"op": "between", "min": 0, "operands": []}""",
            """{"op": "and", "operands": [{"predicate": "a", "extra": true}]}""",
            """{"op": "and"}""",
            "[]",
            "{}",
        ];

    /// <summary>The check accepts and rejects the same JSON rules as a full JSON schema validator.</summary>
    /// <param name="json">The rule.</param>
    [Theory]
    [MemberData(nameof(JsonRules))]
    public void Validate_JsonRule_AgreesWithAJsonSchemaValidator_Test(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        bool expected = Reference.Evaluate(document.RootElement).IsValid;

        IReadOnlyList<Diagnostic> findings = TreeSchemaValidator.Validate(json, RuleFormat.Json);

        Assert.Equal(expected, findings.Count == 0);
    }

    /// <summary>A YAML rule is read with the same scalar typing as the YAML rule reader: a plain number is a number, a quoted one a string.</summary>
    /// <param name="yaml">The rule.</param>
    /// <param name="valid">Whether the rule matches the schema.</param>
    [Theory]
    [InlineData("op: atLeast\nk: 1\noperands:\n  - predicate: a\n", true)]
    [InlineData("op: atLeast\nk: '1'\noperands:\n  - predicate: a\n", false)]
    [InlineData("const: true\n", true)]
    [InlineData("const: 'yes'\n", false)]
    [InlineData("predicate: a\nargs:\n  roles: [x, 2, true]\n", true)]
    [InlineData("predicate: a\nargs:\n  v: ~\n", false)]
    public void Validate_YamlRule_TypesScalarsAsTheRuleReader_Test(string yaml, bool valid)
    {
        IReadOnlyList<Diagnostic> findings = TreeSchemaValidator.Validate(yaml, RuleFormat.Yaml);

        Assert.Equal(valid, findings.Count == 0);
    }

    /// <summary>A finding has the path and the span of the value it is about, also after text that is not ASCII.</summary>
    [Fact]
    public void Validate_FindingAfterNonAsciiText_HasCharacterSpan_Test()
    {
        const string Json = """{"op": "and", "note": "Zürich ✓", "operands": [{"predicate": "a", "k": 2}]}""";

        Diagnostic finding = Assert.Single(TreeSchemaValidator.Validate(Json, RuleFormat.Json));

        Assert.Equal("$.operands[0].k", finding.Path);
        Assert.Equal("2", Json.Substring(finding.Span.Start, finding.Span.Length));
        Assert.Equal(DiagnosticSeverity.Error, finding.Severity);
    }

    /// <summary>A key that is not an identifier is written in brackets in the path.</summary>
    [Fact]
    public void Validate_KeyThatIsNotAnIdentifier_UsesBracketPath_Test()
    {
        Diagnostic finding = Assert.Single(
            TreeSchemaValidator.Validate("""{"predicate": "a", "my key": 1}""", RuleFormat.Json)
        );

        Assert.Equal("$['my key']", finding.Path);
    }

    /// <summary>Text that is not a well-formed document is one malformed-document error.</summary>
    /// <param name="text">The text.</param>
    /// <param name="format">The format.</param>
    [Theory]
    [InlineData("""{"op": "and", """, RuleFormat.Json)]
    [InlineData("op: [and\n", RuleFormat.Yaml)]
    public void Validate_MalformedDocument_ReportsMalformedDocument_Test(string text, RuleFormat format)
    {
        Diagnostic finding = Assert.Single(TreeSchemaValidator.Validate(text, format));

        Assert.Equal(ToolDiagnosticCodes.MalformedDocument, finding.Code);
    }

    /// <summary>The schema uses only the keywords that the check reads, so the check never meets a keyword it ignores.</summary>
    [Fact]
    public void SchemaText_EveryKeyword_IsSupported_Test()
    {
        JsonObject schema = JsonNode.Parse(TreeSchemaValidator.SchemaText())!.AsObject();
        List<string> keywords = [];

        CollectKeywords(schema, keywords);

        Assert.All(keywords, keyword => Assert.Contains(keyword, TreeSchemaValidator.SupportedKeywords));
        Assert.Contains("oneOf", keywords);
    }

    // Walks the schema positions only: the members of "properties" and "$defs" are names, not keywords.
    private static void CollectKeywords(JsonNode? node, List<string> keywords)
    {
        if (node is not JsonObject schema)
        {
            return;
        }

        foreach (KeyValuePair<string, JsonNode?> keyword in schema)
        {
            keywords.Add(keyword.Key);
            switch (keyword.Key)
            {
                case "$defs" or "properties":
                    foreach (KeyValuePair<string, JsonNode?> child in keyword.Value!.AsObject())
                    {
                        CollectKeywords(child.Value, keywords);
                    }

                    break;
                case "oneOf":
                    foreach (JsonNode? child in keyword.Value!.AsArray())
                    {
                        CollectKeywords(child, keywords);
                    }

                    break;
                case "items" or "additionalProperties":
                    CollectKeywords(keyword.Value, keywords);
                    break;
            }
        }
    }
}
