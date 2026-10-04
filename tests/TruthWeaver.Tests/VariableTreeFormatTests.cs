namespace TruthWeaver.Tests;

using System.Text.Json;
using global::Json.Schema;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using TruthWeaver.Yaml;

/// <summary>
/// Variable references in the JSON and YAML tree formats (ADR-0006, data-sources ticket 03): a reference is written
/// <c>{ "from": ..., "query": ... }</c> as an argument value, round-trips to the same canonical text as the DSL, and is
/// described by <c>rule-tree.schema.json</c>.
/// </summary>
public sealed class VariableTreeFormatTests
{
    private const string AgeRule = "ageAtLeast(min: from(\"user\", \"$.minAge\"))";

    private const string AgeJson = """{"predicate":"ageAtLeast","args":{"min":{"from":"user","query":"$.minAge"}}}""";

    private const string AgeYaml = """
        predicate: ageAtLeast
        args:
          min:
            from: user
            query: "$.minAge"
        """;

    private static readonly JsonSchema Schema = RuleTreeSchemaTests.Schema;

    /// <summary>A JSON reference compiles to the same canonical text the DSL gives.</summary>
    [Fact]
    public void CompileJson_VariableReference_HasTheSameCanonicalTextAsTheDsl_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");

        CompilationResult<RuleTestContext> result = compiler.CompileJson(AgeJson);

        Assert.True(result.Succeeded);
        Assert.Equal(AgeRule, result.CompiledRule!.CanonicalText);
    }

    /// <summary>A YAML reference compiles to the same canonical text the DSL gives.</summary>
    [Fact]
    public void CompileYaml_VariableReference_HasTheSameCanonicalTextAsTheDsl_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");

        CompilationResult<RuleTestContext> result = compiler.CompileYaml(AgeYaml);

        Assert.True(result.Succeeded);
        Assert.Equal(AgeRule, result.CompiledRule!.CanonicalText);
    }

    /// <summary>Queries with quotes, backslashes and brackets survive DSL to JSON to YAML and back to the identical text.</summary>
    [Theory]
    [InlineData("$.minAge")]
    [InlineData("$.orders[?@.id=='A7'].total")]
    [InlineData("$.a[?@.n==\"x\\\\y\"]")]
    [InlineData("$['odd key'][0]")]
    public void PrintJsonAndYaml_QueryWithSpecialCharacters_RoundTripsToTheSameCanonicalText_Test(string query)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");
        string escaped = query.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
        string dsl = $"ageAtLeast(min: from(\"user\", \"{escaped}\"))";
        CompiledRule<RuleTestContext> original = compiler.Compile(dsl).CompiledRule!;

        CompiledRule<RuleTestContext> viaJson = compiler.CompileJson(original.PrintJson()).CompiledRule!;
        CompiledRule<RuleTestContext> viaYaml = compiler.CompileYaml(original.PrintYaml()).CompiledRule!;

        Assert.Equal(original.CanonicalText, viaJson.CanonicalText);
        Assert.Equal(original.CanonicalText, viaYaml.CanonicalText);
        Assert.Equal(dsl, viaJson.CanonicalText);
    }

    /// <summary>A variable mixes with literal arguments in one JSON term.</summary>
    [Fact]
    public void CompileJson_VariableAndLiteralArguments_AreBothKept_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");

        CompilationResult<RuleTestContext> result = compiler.CompileJson(
            """{"predicate":"inRange","args":{"max":10,"min":{"from":"user","query":"$.low"}}}"""
        );

        Assert.Equal("inRange(max: 10, min: from(\"user\", \"$.low\"))", result.CompiledRule!.CanonicalText);
    }

    /// <summary>An undeclared source in JSON is reported at the <c>from</c> member, by path and by span.</summary>
    [Fact]
    public void CompileJson_UndeclaredSource_PointsAtTheFromMember_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("request");

        CompilationResult<RuleTestContext> result = compiler.CompileJson(AgeJson);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.UndeclaredDataSource);
        Assert.Equal("$.args.min.from", diagnostic.Path);
        Assert.Equal(AgeJson.IndexOf("\"user\"", StringComparison.Ordinal), diagnostic.Span.Start);
        Assert.Equal("\"user\"".Length, diagnostic.Span.Length);
    }

    /// <summary>An undeclared source in YAML is reported at the <c>from</c> member, by path and by span.</summary>
    [Fact]
    public void CompileYaml_UndeclaredSource_PointsAtTheFromMember_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("request");

        CompilationResult<RuleTestContext> result = compiler.CompileYaml(AgeYaml);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCodes.UndeclaredDataSource);
        Assert.Equal("$.args.min.from", diagnostic.Path);
        Assert.Equal(AgeYaml.IndexOf("user", StringComparison.Ordinal), diagnostic.Span.Start);
    }

    /// <summary>A malformed reference object is a tree diagnostic at the member that is wrong.</summary>
    [Theory]
    [InlineData("""{"predicate":"ageAtLeast","args":{"min":{"from":"user"}}}""", "$.args.min")]
    [InlineData("""{"predicate":"ageAtLeast","args":{"min":{"query":"$.a"}}}""", "$.args.min")]
    [InlineData("""{"predicate":"ageAtLeast","args":{"min":{"from":1,"query":"$.a"}}}""", "$.args.min.from")]
    [InlineData("""{"predicate":"ageAtLeast","args":{"min":{"from":"user","query":7}}}""", "$.args.min.query")]
    [InlineData("""{"predicate":"ageAtLeast","args":{"min":{"from":"user","query":"$.a","extra":1}}}""", "$.args.min.extra")]
    [InlineData("""{"predicate":"ageAtLeast","args":{"min":{}}}""", "$.args.min")]
    [InlineData("""{"predicate":"ageAtLeast","args":{"min":[{"from":"user","query":"$.a"}]}}""", "$.args.min[0]")]
    public void CompileJson_MalformedReference_ReportsMalformedTreeAtTheOffendingMember_Test(string json, string path)
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");

        CompilationResult<RuleTestContext> result = compiler.CompileJson(json);

        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.MalformedTree, diagnostic.Code);
        Assert.Equal(path, diagnostic.Path);
    }

    /// <summary>A reference object with a missing query is rejected in YAML too.</summary>
    [Fact]
    public void CompileYaml_ReferenceWithoutQuery_ReportsMalformedTree_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");

        CompilationResult<RuleTestContext> result = compiler.CompileYaml(
            "predicate: ageAtLeast\nargs:\n  min:\n    from: user\n"
        );

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.MalformedTree, diagnostic.Code);
        Assert.Equal("$.args.min", diagnostic.Path);
    }

    /// <summary>The printed JSON of a rule with a reference validates against the published schema.</summary>
    [Fact]
    public void Schema_PrintedVariableReference_Validates_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");
        string json = compiler.Compile(AgeRule).CompiledRule!.PrintJson();
        using JsonDocument document = JsonDocument.Parse(json);

        Assert.True(Schema.Evaluate(document.RootElement).IsValid);
    }

    /// <summary>The schema rejects references that are missing a member, carry an extra one or have a non-string member.</summary>
    [Theory]
    [InlineData("""{"predicate":"p","args":{"a":{"from":"user"}}}""")]
    [InlineData("""{"predicate":"p","args":{"a":{"query":"$.a"}}}""")]
    [InlineData("""{"predicate":"p","args":{"a":{"from":"user","query":"$.a","extra":1}}}""")]
    [InlineData("""{"predicate":"p","args":{"a":{"from":1,"query":"$.a"}}}""")]
    [InlineData("""{"predicate":"p","args":{"a":{"from":"user","query":null}}}""")]
    [InlineData("""{"predicate":"p","args":{"a":[{"from":"user","query":"$.a"}]}}""")]
    public void Schema_MalformedVariableReference_FailsValidation_Test(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        Assert.False(Schema.Evaluate(document.RootElement).IsValid);
    }

    /// <summary>The schema accepts what the parser accepts for a reference, so a compiled document is never schema-invalid.</summary>
    [Fact]
    public void Schema_VariableReference_AgreesWithTheParser_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");
        using JsonDocument document = JsonDocument.Parse(AgeJson);

        Assert.True(compiler.CompileJson(AgeJson).Succeeded);
        Assert.True(Schema.Evaluate(document.RootElement).IsValid);
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler(params string[] declaredSources)
    {
        DataSourceDeclarations declarations = [];
        foreach (string name in declaredSources)
        {
            declarations.Add(name);
        }

        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .Add(
                new PredicateSchema(
                    "ageAtLeast",
                    "Age at least",
                    "Test predicate, always true.",
                    [new PredicateArgumentSchema("min", "The minimum age.", LiteralKind.Int64)]
                ),
                (_, _, _) => ValueTask.FromResult(TruthValue.True)
            )
            .Add(
                new PredicateSchema(
                    "inRange",
                    "In range",
                    "Test predicate, always true.",
                    [
                        new PredicateArgumentSchema("min", "The lower bound.", LiteralKind.Int64),
                        new PredicateArgumentSchema("max", "The upper bound.", LiteralKind.Int64),
                    ]
                ),
                (_, _, _) => ValueTask.FromResult(TruthValue.True)
            )
            .Build();
        return new RuleCompiler<RuleTestContext>(registry, new CompilerOptions(DataSources: declarations));
    }
}
