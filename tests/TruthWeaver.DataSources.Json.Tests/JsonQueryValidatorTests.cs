namespace TruthWeaver.DataSources.Json.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;

/// <summary>
/// <see cref="JsonQueryValidator"/> (ADR-0006 decision 4, data-sources ticket 05): RFC 9535 syntax checking without a
/// document, used standalone and as a source's compile-time validator.
/// </summary>
public sealed class JsonQueryValidatorTests
{
    /// <summary>Gets valid RFC 9535 queries, covering names, indexes, slices, wildcards, descendants, filters and functions.</summary>
    public static TheoryData<string> ValidQueries =>
        [
            "$",
            "$.minAge",
            "$['odd key']",
            "$.items[0].price",
            "$.items[-1]",
            "$.items[1:3]",
            "$.roles[*]",
            "$..name",
            "$.orders[?@.id=='A7'].total",
            "$.orders[?@.total > 100 && @.open]",
            "$.orders[?length(@.lines) == 2]",
            "$.a[?match(@.b, 'x.*')]",
            "$.a[0,2]",
        ];

    /// <summary>Gets queries that are not RFC 9535.</summary>
    public static TheoryData<string> InvalidQueries =>
        [string.Empty, "minAge", "$.", "$.orders[", "$.orders[?@.id==]", "$[1:2:3:4]"];

    /// <summary>A well-formed query yields no problems.</summary>
    [Theory]
    [MemberData(nameof(ValidQueries))]
    public void Validate_ValidRfc9535Query_ReturnsNoProblems_Test(string query)
    {
        IReadOnlyList<QueryProblem> problems = JsonQueryValidator.Instance.Validate(query);

        Assert.Empty(problems);
    }

    /// <summary>A malformed query yields one problem with a message and a position that lies within the query.</summary>
    [Theory]
    [MemberData(nameof(InvalidQueries))]
    public void Validate_MalformedQuery_ReportsAMessageAndAPositionWithinTheQuery_Test(string query)
    {
        IReadOnlyList<QueryProblem> problems = JsonQueryValidator.Instance.Validate(query);

        QueryProblem problem = Assert.Single(problems);
        Assert.False(string.IsNullOrWhiteSpace(problem.Message));
        Assert.InRange(problem.Position ?? -1, 0, query.Length);
    }

    /// <summary>Where the failure is unambiguous the position is exact: a query not starting with <c>$</c> fails at 0 and an unclosed bracket at its end.</summary>
    [Theory]
    [InlineData("minAge", 0)]
    [InlineData("$.orders[", 9)]
    public void Validate_UnambiguousFailure_ReportsTheExactPosition_Test(string query, int position)
    {
        QueryProblem problem = Assert.Single(JsonQueryValidator.Instance.Validate(query));

        Assert.Equal(position, problem.Position);
    }

    /// <summary>A query that stops right after a member dot is a malformed query ending at the end of the text, never an exception.</summary>
    [Theory]
    [InlineData("$.")]
    [InlineData("$.orders[?@.open].")]
    public async Task Validate_QueryEndingAfterADot_ReportsAProblemAtTheEndOfTheQuery_Test(string query)
    {
        QueryProblem problem = Assert.Single(JsonQueryValidator.Instance.Validate(query));
        DataQueryResult result = await JsonDataSource
            .Parse("{}")
            .QueryAsync(query, TestContext.Current.CancellationToken);

        Assert.Equal(query.Length, problem.Position);
        Assert.Equal(DataQueryErrorKind.MalformedQuery, result.ErrorKind);
    }

    /// <summary>The validator needs no document, so the same shared instance serves every query.</summary>
    [Fact]
    public void Instance_IsShared_Test()
    {
        Assert.Same(JsonQueryValidator.Instance, JsonQueryValidator.Instance);
    }

    /// <summary>The data source and the validator agree: a query the validator accepts is never reported malformed by the source.</summary>
    [Theory]
    [MemberData(nameof(ValidQueries))]
    public async Task QueryAsync_QueryTheValidatorAccepts_IsNotReportedMalformed_Test(string query)
    {
        JsonDataSource source = JsonDataSource.Parse("""{ "a": 1 }""");

        DataQueryResult result = await source.QueryAsync(query, TestContext.Current.CancellationToken);

        Assert.NotEqual(DataQueryErrorKind.MalformedQuery, result.ErrorKind);
    }

    /// <summary>A DSL rule whose source is declared with the validator fails to compile on a malformed JSONPath, at the query string.</summary>
    [Fact]
    public void Compile_MalformedJsonPathInDsl_IsADiagnosticAtTheQuery_Test()
    {
        RuleCompiler<object?> compiler = CreateCompiler();
        const string rule = "takesInt64(v: from(\"doc\", \"$.orders[\"))";

        CompilationResult<object?> result = compiler.Compile(rule);

        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.MalformedDataQuery, diagnostic.Code);
        Assert.Equal(rule.IndexOf("\"$.orders[\"", StringComparison.Ordinal), diagnostic.Span.Start);
    }

    /// <summary>A JSON rule with a malformed JSONPath is a diagnostic at the <c>query</c> member.</summary>
    [Fact]
    public void CompileJson_MalformedJsonPath_IsADiagnosticAtTheQueryMember_Test()
    {
        RuleCompiler<object?> compiler = CreateCompiler();
        const string rule = """{"predicate":"takesInt64","args":{"v":{"from":"doc","query":"$.orders["}}}""";

        CompilationResult<object?> result = compiler.CompileJson(rule);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.MalformedDataQuery, diagnostic.Code);
        Assert.Equal("$.args.v.query", diagnostic.Path);
        Assert.Equal(rule.IndexOf("\"$.orders[\"", StringComparison.Ordinal), diagnostic.Span.Start);
    }

    /// <summary>A valid JSONPath compiles, and the same bad query against a source declared without a validator also compiles.</summary>
    [Fact]
    public void Compile_ValidQueryOrUnvalidatedSource_ProducesNoDiagnostic_Test()
    {
        RuleCompiler<object?> compiler = CreateCompiler();

        CompilationResult<object?> valid = compiler.Compile("takesInt64(v: from(\"doc\", \"$.orders[0].total\"))");
        CompilationResult<object?> unvalidated = compiler.Compile("takesInt64(v: from(\"loose\", \"$.orders[\"))");

        Assert.True(valid.Succeeded);
        Assert.True(unvalidated.Succeeded);
    }

    private static RuleCompiler<object?> CreateCompiler()
    {
        PredicateRegistry<object?> registry = PredicateRegistry<object?>
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
        DataSourceDeclarations declarations = new() { ["doc"] = JsonQueryValidator.Instance, ["loose"] = null };
        return new RuleCompiler<object?>(registry, new CompilerOptions(DataSources: declarations));
    }
}
