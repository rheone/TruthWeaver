namespace TruthWeaver.Tests;

using NSubstitute;
using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;
using Arg = TruthWeaver.Building.Arg;

/// <summary>
/// <see cref="RuleBuilder"/> support for data sources (ADR-0006 decision 12, data-sources ticket 08): <see cref="Arg.From"/>
/// is a deferred variable reference, and <see cref="DataSourceExtensions.GetAsync{T}"/> reads a value while the rule is
/// assembled and inserts an ordinary literal.
/// </summary>
public sealed class RuleBuilderVariableTests
{
    private const string AgeRule = "ageAtLeast(min: from(\"user\", \"$.minAge\"))";

    /// <summary>A builder rule using <see cref="Arg.From"/> compiles to the same canonical text as the DSL form.</summary>
    [Fact]
    public void Compile_PredicateWithArgFrom_HasTheSameCanonicalTextAsTheDsl_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("user");

        CompilationResult<RuleTestContext> result = RuleBuilder
            .Predicate("ageAtLeast", ("min", Arg.From("user", "$.minAge")))
            .Compile(compiler);

        Assert.True(result.Succeeded);
        Assert.Equal(compiler.Compile(AgeRule).CompiledRule!.CanonicalText, result.CompiledRule!.CanonicalText);
        Assert.Equal(AgeRule, result.CompiledRule.CanonicalText);
    }

    /// <summary>A reference renders as the same JSON object the JSON rule format reads.</summary>
    [Fact]
    public void ToJson_PredicateWithArgFrom_RendersTheFromObject_Test()
    {
        string json = RuleBuilder.Predicate("ageAtLeast", ("min", Arg.From("user", "$.minAge"))).ToJson();

        Assert.Equal("""{"predicate":"ageAtLeast","args":{"min":{"from":"user","query":"$.minAge"}}}""", json);
    }

    /// <summary>A builder reference to a source that was not declared is the same compile diagnostic as in rule text.</summary>
    [Fact]
    public void Compile_ArgFromUndeclaredSource_ReportsTheUndeclaredSourceDiagnostic_Test()
    {
        RuleCompiler<RuleTestContext> compiler = CreateCompiler("request");

        CompilationResult<RuleTestContext> result = RuleBuilder
            .Predicate("ageAtLeast", ("min", Arg.From("user", "$.minAge")))
            .Compile(compiler);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.UndeclaredDataSource);
    }

    /// <summary>A supplied validator that accepts the query lets the reference through.</summary>
    [Fact]
    public void From_ValidatorAcceptsTheQuery_ReturnsTheReference_Test()
    {
        IQueryValidator validator = Substitute.For<IQueryValidator>();
        validator.Validate("$.minAge").Returns([]);

        VariableReference reference = Arg.From("user", "$.minAge", validator);

        Assert.Equal(new VariableReference("user", "$.minAge"), reference);
    }

    /// <summary>A supplied validator that reports a problem makes <see cref="Arg.From"/> throw, naming the problem.</summary>
    [Fact]
    public void From_ValidatorRejectsTheQuery_ThrowsArgumentExceptionWithTheProblem_Test()
    {
        IQueryValidator validator = Substitute.For<IQueryValidator>();
        validator.Validate("$.orders[").Returns([new QueryProblem("unterminated bracket", 8)]);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => Arg.From("user", "$.orders[", validator));

        Assert.Contains("unterminated bracket", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>A missing source name or query is a programming error, rejected at once.</summary>
    [Fact]
    public void From_NullSourceOrQuery_Throws_Test()
    {
        Assert.Throws<ArgumentNullException>(() => Arg.From(null!, "$.a"));
        Assert.Throws<ArgumentNullException>(() => Arg.From("user", null!));
    }

    /// <summary>The eager helper's result is an ordinary literal argument: the rule has no variable and needs no source.</summary>
    [Fact]
    public async Task GetAsync_Int64Match_ProducesAnOrdinaryLiteralArgument_Test()
    {
        StubSource source = SourceReturning(LiteralValue.OfInt64(18));
        RuleCompiler<RuleTestContext> compiler = CreateCompiler();

        long limit = await source.GetAsync<long>("$.limits.age", TestContext.Current.CancellationToken);
        CompilationResult<RuleTestContext> result = RuleBuilder.Predicate("ageAtLeast", ("min", limit)).Compile(compiler);

        Assert.Equal(18, limit);
        Assert.True(result.Succeeded);
        Assert.Equal("ageAtLeast(min: 18)", result.CompiledRule!.CanonicalText);
    }

    /// <summary>A string match is a string, and a string that is a GUID or timestamp converts like a DSL literal.</summary>
    [Fact]
    public async Task GetAsync_StringMatch_ConvertsToGuidAndDateTimeOffsetLikeALiteral_Test()
    {
        Guid id = Guid.NewGuid();
        DateTimeOffset when = new(2026, 10, 3, 0, 0, 0, TimeSpan.Zero);
        CancellationToken token = TestContext.Current.CancellationToken;

        Guid guid = await SourceReturning(LiteralValue.OfString(id.ToString())).GetAsync<Guid>("$.id", token);
        DateTimeOffset moment = await SourceReturning(LiteralValue.OfString(when.ToString("O")))
            .GetAsync<DateTimeOffset>("$.at", token);
        string text = await SourceReturning(LiteralValue.OfString("admin")).GetAsync<string>("$.role", token);

        Assert.Equal(id, guid);
        Assert.Equal(when, moment);
        Assert.Equal("admin", text);
    }

    /// <summary>A Guid read at build time can be passed straight back to the builder.</summary>
    [Fact]
    public void Predicate_GuidArgument_RendersAsAString_Test()
    {
        Guid id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        string json = RuleBuilder.Predicate("p", ("id", id)).ToJson();

        Assert.Equal("""{"predicate":"p","args":{"id":"11111111-2222-3333-4444-555555555555"}}""", json);
    }

    /// <summary>No match, several, the wrong kind, or a failing source is an <see cref="InvalidOperationException"/> that does not echo data.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task GetAsync_NoSingleConvertibleMatch_ThrowsInvalidOperationWithoutTheData_Test(int scenario)
    {
        IDataSource source = scenario switch
        {
            0 => SourceReturning(),
            2 => SourceReturning(LiteralValue.OfInt64(1), LiteralValue.OfInt64(2)),
            3 => SourceReturning(LiteralValue.OfString("hunter2")),
            _ => FailingSource(),
        };

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await source.GetAsync<long>("$.x", TestContext.Current.CancellationToken)
        );

        Assert.DoesNotContain("hunter2", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>A type with no literal kind is rejected rather than guessed at.</summary>
    [Fact]
    public Task GetAsync_UnsupportedType_ThrowsNotSupported_Test()
    {
        StubSource source = SourceReturning(LiteralValue.OfInt64(1));

        return Assert.ThrowsAsync<NotSupportedException>(async () =>
            await source.GetAsync<int>("$.x", TestContext.Current.CancellationToken)
        );
    }

    /// <summary><see cref="Arg.TryFrom"/> with a validator that accepts the query returns the same reference as <see cref="Arg.From"/>.</summary>
    [Fact]
    public void TryFrom_ValidatorAcceptsTheQuery_ReturnsTheReferenceAndNoProblems_Test()
    {
        IQueryValidator validator = Substitute.For<IQueryValidator>();
        validator.Validate("$.minAge").Returns([]);

        bool ok = Arg.TryFrom(
            "user",
            "$.minAge",
            validator,
            out VariableReference? reference,
            out IReadOnlyList<QueryProblem> problems
        );

        Assert.True(ok);
        Assert.Equal(new VariableReference("user", "$.minAge"), reference);
        Assert.Empty(problems);
    }

    /// <summary>A validator rejection is returned as problems and never thrown.</summary>
    [Fact]
    public void TryFrom_ValidatorRejectsTheQuery_ReturnsFalseWithTheProblemsAndNoReference_Test()
    {
        IQueryValidator validator = Substitute.For<IQueryValidator>();
        validator.Validate("$.orders[").Returns([new QueryProblem("unterminated bracket", 8)]);

        bool ok = Arg.TryFrom(
            "user",
            "$.orders[",
            validator,
            out VariableReference? reference,
            out IReadOnlyList<QueryProblem> problems
        );

        Assert.False(ok);
        Assert.Null(reference);
        Assert.Equal("unterminated bracket", Assert.Single(problems).Message);
    }

    /// <summary>Without a validator the query is unchecked, so <see cref="Arg.TryFrom"/> succeeds.</summary>
    [Fact]
    public void TryFrom_NoValidator_ReturnsTheReference_Test()
    {
        bool ok = Arg.TryFrom("user", "$.a", null, out VariableReference? reference, out IReadOnlyList<QueryProblem> problems);

        Assert.True(ok);
        Assert.NotNull(reference);
        Assert.Empty(problems);
    }

    /// <summary>A null source or query is a programming error, so <see cref="Arg.TryFrom"/> still throws.</summary>
    [Fact]
    public void TryFrom_NullSourceOrQuery_Throws_Test()
    {
        Assert.Throws<ArgumentNullException>(() => Arg.TryFrom(null!, "$.a", null, out _, out _));
        Assert.Throws<ArgumentNullException>(() => Arg.TryFrom("user", null!, null, out _, out _));
    }

    /// <summary>A single convertible match is a success carrying the value.</summary>
    [Fact]
    public async Task TryGetAsync_Int64Match_SucceedsWithTheValue_Test()
    {
        DataReadResult<long> result = await SourceReturning(LiteralValue.OfInt64(18))
            .TryGetAsync<long>("$.limits.age", TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(18, result.Value);
        Assert.Null(result.FailureKind);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>A string match converts to a Guid like a variable would.</summary>
    [Fact]
    public async Task TryGetAsync_StringMatch_ConvertsToGuid_Test()
    {
        Guid id = Guid.NewGuid();

        DataReadResult<Guid> result = await SourceReturning(LiteralValue.OfString(id.ToString()))
            .TryGetAsync<Guid>("$.id", TestContext.Current.CancellationToken);

        Assert.True(result.Succeeded);
        Assert.Equal(id, result.Value);
    }

    /// <summary>Each failure kind is a failure result, never an exception, and the message holds no data.</summary>
    [Theory]
    [InlineData(0, VariableFailureKind.Missing)]
    [InlineData(1, VariableFailureKind.Ambiguous)]
    [InlineData(2, VariableFailureKind.TypeMismatch)]
    [InlineData(3, VariableFailureKind.SourceError)]
    public async Task TryGetAsync_NoSingleConvertibleMatch_ReturnsFailureWithoutTheData_Test(
        int scenario,
        VariableFailureKind expected
    )
    {
        IDataSource source = scenario switch
        {
            0 => SourceReturning(),
            1 => SourceReturning(LiteralValue.OfInt64(1), LiteralValue.OfInt64(2)),
            2 => SourceReturning(LiteralValue.OfString("hunter2")),
            _ => FailingSource(),
        };

        DataReadResult<long> result = await source.TryGetAsync<long>("$.x", TestContext.Current.CancellationToken);

        Assert.False(result.Succeeded);
        Assert.Equal(expected, result.FailureKind);
        Assert.DoesNotContain("hunter2", result.ErrorMessage, StringComparison.Ordinal);
    }

    /// <summary>An unsupported type is a programmer error, so the result form throws like <c>GetAsync</c>.</summary>
    [Fact]
    public Task TryGetAsync_UnsupportedType_ThrowsNotSupported_Test()
    {
        StubSource source = SourceReturning(LiteralValue.OfInt64(1));

        return Assert.ThrowsAsync<NotSupportedException>(async () =>
            await source.TryGetAsync<int>("$.x", TestContext.Current.CancellationToken)
        );
    }

    private static StubSource SourceReturning(params LiteralValue[] matches)
    {
        return new StubSource(DataQueryResult.Success(matches));
    }

    private static StubSource FailingSource()
    {
        return new StubSource(DataQueryResult.Failure(DataQueryErrorKind.SourceFailure, "offline"));
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
            .Build();
        return new RuleCompiler<RuleTestContext>(registry, new CompilerOptions(DataSources: declarations));
    }

    /// <summary>A data source that always gives one canned answer.</summary>
    private sealed class StubSource(DataQueryResult result) : IDataSource
    {
        public ValueTask<DataQueryResult> QueryAsync(string query, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(result);
        }

        public ValueTask<DataScopeResult> ScopeAsync(string query, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
