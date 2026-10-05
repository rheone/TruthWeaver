namespace TruthWeaver.Yaml.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.DataSources.Json;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Yaml;
using TruthWeaver.Yaml.Tests.TestSupport;
using YamlDotNet.Core;

/// <summary>
/// <see cref="YamlDataSource"/> (ADR-0006 decisions 5 and 14, data-sources ticket 06): a YAML document is read into the JSON
/// data model, so the same JSONPath query gives the same result as against the equivalent JSON, and the JSON package's validator
/// serves YAML rules too.
/// </summary>
public sealed class YamlDataSourceTests
{
    private const string OrdersJson = """
        {
          "limits": { "age": 18, "ratio": 1.5, "whole": 18.0, "name": "adult", "numeric": "18", "on": true, "off": false, "nothing": null },
          "roles": ["admin", "auditor"],
          "orders": [
            { "id": "A7", "total": 120, "lines": [{ "sku": "x" }, { "sku": "y" }] },
            { "id": "B2", "total": 80, "lines": [{ "sku": "z" }] }
          ]
        }
        """;

    private const string OrdersYaml = """
        # the same document as OrdersJson
        limits:
          age: 18
          ratio: 1.5
          whole: 18.0
          name: adult
          numeric: "18"
          "on": true
          "off": false
          nothing: null
        roles: [admin, auditor]
        orders:
          - id: A7
            total: 120
            lines:
              - sku: x
              - sku: y
          - id: B2
            total: 80
            lines:
              - { sku: z }
        """;

    /// <summary>Gets queries covering every node kind, every cardinality and the error results.</summary>
    public static TheoryData<string> EquivalenceQueries =>
        [
            "$.limits.age",
            "$.limits.ratio",
            "$.limits.whole",
            "$.limits.name",
            "$.limits.numeric",
            "$.limits.on",
            "$.limits.off",
            "$.limits.nothing",
            "$.limits",
            "$.roles",
            "$.roles[*]",
            "$.orders[?@.id=='A7'].total",
            "$.orders[*].total",
            "$.orders[*].lines[*].sku",
            "$.missing",
            "$.orders[",
        ];

    /// <summary>The same query gives the same result against equivalent JSON and YAML documents, including the error kind.</summary>
    [Theory]
    [MemberData(nameof(EquivalenceQueries))]
    public async Task QueryAsync_EquivalentJsonAndYaml_GiveTheSameResult_Test(string query)
    {
        JsonDataSource json = JsonDataSource.Parse(OrdersJson);
        YamlDataSource yaml = YamlDataSource.Parse(OrdersYaml);

        DataQueryResult fromJson = await json.QueryAsync(query, TestContext.Current.CancellationToken);
        DataQueryResult fromYaml = await yaml.QueryAsync(query, TestContext.Current.CancellationToken);

        Assert.Equal(fromJson.ErrorKind, fromYaml.ErrorKind);
        Assert.Equal(fromJson.Matches, fromYaml.Matches);
    }

    /// <summary>A quoted scalar is a string even when it looks like a number, and a plain one is a number.</summary>
    [Fact]
    public async Task QueryAsync_QuotedNumberLookalike_IsAStringAndPlainIsAnInteger_Test()
    {
        YamlDataSource yaml = YamlDataSource.Parse("quoted: \"18\"\nplain: 18\ntagged: !!str 18\n");

        DataQueryResult quoted = await yaml.QueryAsync("$.quoted", TestContext.Current.CancellationToken);
        DataQueryResult plain = await yaml.QueryAsync("$.plain", TestContext.Current.CancellationToken);
        DataQueryResult tagged = await yaml.QueryAsync("$.tagged", TestContext.Current.CancellationToken);

        Assert.Equal(LiteralValue.OfString("18"), Assert.Single(quoted.Matches));
        Assert.Equal(LiteralValue.OfInt64(18), Assert.Single(plain.Matches));
        Assert.Equal(LiteralValue.OfString("18"), Assert.Single(tagged.Matches));
    }

    /// <summary>Plain scalars read as the core-schema values: booleans in any case, <c>~</c> and an empty value as null, other text as a string.</summary>
    [Theory]
    [InlineData("v: True", "true")]
    [InlineData("v: FALSE", "false")]
    [InlineData("v: hello world", "\"hello world\"")]
    [InlineData("v: 2026-10-03", "\"2026-10-03\"")]
    [InlineData("v: -5", "-5")]
    [InlineData("v: 1e3", "1000")]
    [InlineData("v: 007", "\"007\"")]
    [InlineData("v: yes", "\"yes\"")]
    public async Task QueryAsync_PlainScalar_ReadsAsTheCoreSchemaValue_Test(string yaml, string expectedLiteral)
    {
        YamlDataSource source = YamlDataSource.Parse(yaml);

        DataQueryResult result = await source.QueryAsync("$.v", TestContext.Current.CancellationToken);

        Assert.Equal(expectedLiteral, Assert.Single(result.Matches).ToString());
    }

    /// <summary>A null, a tilde and an empty plain value are all null, which has no literal equivalent.</summary>
    [Theory]
    [InlineData("v: null")]
    [InlineData("v: ~")]
    [InlineData("v:")]
    public async Task QueryAsync_NullSpellings_AreUnsupportedTypes_Test(string yaml)
    {
        YamlDataSource source = YamlDataSource.Parse(yaml);

        DataQueryResult result = await source.QueryAsync("$.v", TestContext.Current.CancellationToken);

        Assert.Equal(DataQueryErrorKind.UnsupportedType, result.ErrorKind);
    }

    /// <summary>An alias reads as a copy of its anchored node.</summary>
    [Fact]
    public async Task QueryAsync_AliasedNode_ReadsAsACopyOfTheAnchor_Test()
    {
        YamlDataSource yaml = YamlDataSource.Parse("base: &limit\n  age: 21\ncopy: *limit\n");

        DataQueryResult result = await yaml.QueryAsync("$.copy.age", TestContext.Current.CancellationToken);

        Assert.Equal(LiteralValue.OfInt64(21), Assert.Single(result.Matches));
    }

    /// <summary>Only the first document of a multi-document stream is read; an empty stream is a source with no data.</summary>
    [Fact]
    public async Task Parse_MultipleOrNoDocuments_ReadsTheFirstOrNothing_Test()
    {
        YamlDataSource first = YamlDataSource.Parse("a: 1\n---\na: 2\n");
        YamlDataSource none = YamlDataSource.Parse(string.Empty);

        DataQueryResult firstResult = await first.QueryAsync("$.a", TestContext.Current.CancellationToken);
        DataQueryResult noneResult = await none.QueryAsync("$.a", TestContext.Current.CancellationToken);

        Assert.Equal(LiteralValue.OfInt64(1), Assert.Single(firstResult.Matches));
        Assert.True(noneResult.Succeeded);
        Assert.Empty(noneResult.Matches);
    }

    /// <summary>A mapping key that is not text is read as its text, as JSON would require.</summary>
    [Fact]
    public async Task QueryAsync_NumericMappingKey_IsReadAsText_Test()
    {
        YamlDataSource yaml = YamlDataSource.Parse("1: one\n2: two\n");

        DataQueryResult result = await yaml.QueryAsync("$['2']", TestContext.Current.CancellationToken);

        Assert.Equal(LiteralValue.OfString("two"), Assert.Single(result.Matches));
    }

    /// <summary>Malformed YAML and a self-referencing alias are rejected when the source is created.</summary>
    [Theory]
    [InlineData("a: [1, 2")]
    [InlineData("a: &x [*x]")]
    [InlineData("a: 1\na: 2")]
    public void Parse_MalformedOrCyclicYaml_ThrowsYamlException_Test(string yaml)
    {
        Assert.ThrowsAny<YamlException>(() => YamlDataSource.Parse(yaml));
    }

    /// <summary>Malformed YAML, a duplicate key and a self-referencing alias each make <c>TryParse</c> return false with an error, and never throw.</summary>
    [Theory]
    [InlineData("a: [1, 2")]
    [InlineData("a: &x [*x]")]
    [InlineData("a: 1\na: 2")]
    public void TryParse_MalformedOrCyclicYaml_ReturnsFalseWithError_Test(string yaml)
    {
        bool parsed = YamlDataSource.TryParse(yaml, out YamlDataSource? source, out string? error);

        Assert.False(parsed);
        Assert.Null(source);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    /// <summary>The error names where the document is wrong but never repeats document content, which may be sensitive.</summary>
    [Fact]
    public void TryParse_DuplicateKey_ErrorDoesNotEchoDocumentContent_Test()
    {
        Assert.False(YamlDataSource.TryParse("hunter2: 1\nhunter2: 2", out _, out string? error));

        Assert.NotNull(error);
        Assert.DoesNotContain("hunter2", error);
    }

    /// <summary>A well-formed document gives a source that answers queries exactly as <c>Parse</c> does.</summary>
    [Fact]
    public async Task TryParse_WellFormedYaml_ReturnsAWorkingSource_Test()
    {
        bool parsed = YamlDataSource.TryParse("a: 1\n", out YamlDataSource? source, out string? error);

        Assert.True(parsed);
        Assert.Null(error);
        DataQueryResult result = await source!.QueryAsync("$.a", TestContext.Current.CancellationToken);
        Assert.Equal(LiteralValue.OfInt64(1), Assert.Single(result.Matches));
    }

    /// <summary>A null document is a programming error and still throws, as it does for <c>Parse</c>.</summary>
    [Fact]
    public void TryParse_NullText_ThrowsArgumentNullException_Test()
    {
        Assert.Throws<ArgumentNullException>(() => YamlDataSource.TryParse(null!, out _, out _));
    }

    /// <summary>A source scoped to one order answers absolute queries within that order, as the JSON source does.</summary>
    [Fact]
    public async Task ScopeAsync_OneOrder_RootsQueriesAtThatOrder_Test()
    {
        YamlDataSource yaml = YamlDataSource.Parse(OrdersYaml);

        DataScopeResult scope = await yaml.ScopeAsync("$.orders[?@.id=='B2']", TestContext.Current.CancellationToken);
        Assert.True(scope.Succeeded, scope.ErrorMessage);
        IDataSource order = scope.Source;
        DataQueryResult total = await order.QueryAsync("$.total", TestContext.Current.CancellationToken);

        Assert.Equal(LiteralValue.OfInt64(80), Assert.Single(total.Matches));
    }

    /// <summary>A scope query that matches none, several or is malformed gives a failure result, as the JSON source does.</summary>
    [Theory]
    [InlineData("$.orders[?@.id=='nope']", DataQueryErrorKind.NoMatch)]
    [InlineData("$.orders[*]", DataQueryErrorKind.AmbiguousMatch)]
    [InlineData("$.orders[", DataQueryErrorKind.MalformedQuery)]
    public async Task ScopeAsync_QueryNotMatchingExactlyOneNode_ReturnsFailureResult_Test(
        string query,
        DataQueryErrorKind expected
    )
    {
        YamlDataSource yaml = YamlDataSource.Parse(OrdersYaml);

        DataScopeResult result = await yaml.ScopeAsync(query, TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(expected, result.ErrorKind);
    }

    /// <summary>A YAML rule declared with the JSON validator reports a malformed JSONPath at its query string.</summary>
    [Fact]
    public void CompileYaml_MalformedJsonPath_IsADiagnosticAtTheQuery_Test()
    {
        RuleCompiler<YamlTestContext> compiler = CreateCompiler();
        const string rule = "predicate: takesInt64\nargs:\n  v:\n    from: doc\n    query: \"$.orders[\"\n";

        CompilationResult<YamlTestContext> result = compiler.CompileYaml(rule);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.MalformedDataQuery, diagnostic.Code);
        Assert.Equal("$.args.v.query", diagnostic.Path);
        Assert.Equal(rule.IndexOf("\"$.orders[\"", StringComparison.Ordinal), diagnostic.Span.Start);
    }

    /// <summary>End to end: a YAML rule reading a YAML document, a valid query compiles and resolves.</summary>
    [Fact]
    public async Task EvaluateAsync_YamlRuleOverYamlDocument_ResolvesTheVariable_Test()
    {
        RuleCompiler<YamlTestContext> compiler = CreateCompiler();
        CompilationResult<YamlTestContext> result = compiler.CompileYaml(
            "predicate: takesInt64\nargs:\n  v:\n    from: doc\n    query: \"$.orders[?@.id=='A7'].total\"\n"
        );
        DataSources sources = new() { ["doc"] = YamlDataSource.Parse(OrdersYaml) };

        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new YamlTestContext(),
            EmptyServices.Instance,
            sources,
            null,
            TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
        Assert.Empty(decision.Faults);
    }

    private static RuleCompiler<YamlTestContext> CreateCompiler()
    {
        PredicateRegistry<YamlTestContext> registry = PredicateRegistry<YamlTestContext>
            .CreateBuilder()
            .Add(
                new PredicateSchema(
                    "takesInt64",
                    "Takes an integer",
                    "Always true.",
                    [new PredicateArgumentSchema("v", "The value.", LiteralKind.Int64)]
                ),
                (_, _, _) => ValueTask.FromResult(TruthValue.True)
            )
            .Build();
        DataSourceDeclarations declarations = new() { ["doc"] = JsonQueryValidator.Instance };
        return new RuleCompiler<YamlTestContext>(registry, new CompilerOptions(DataSources: declarations));
    }

    private sealed class EmptyServices : IServiceProvider
    {
        public static EmptyServices Instance { get; } = new();

        public object? GetService(Type serviceType)
        {
            return null;
        }
    }
}
