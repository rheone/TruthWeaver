namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Predicates;
using TruthWeaver.Registry;
using TruthWeaver.Yaml;

/// <summary>
/// A date-time text must carry <c>Z</c> or an offset, in a rule literal of every front end and in a value a data
/// source resolves, so a stored rule means the same instant on every host.
/// </summary>
[Collection(HostTimeZoneCollection.Name)]
public sealed class DateTimeLiteralOffsetTests
{
    private const string Instant = "\"2026-02-01T00:00:00Z\"";

    /// <summary>Offset-less text in a DSL literal is a type-mismatch error that names the fix.</summary>
    [Theory]
    [InlineData("2026-01-01")]
    [InlineData("2026-01-01T09:00")]
    [InlineData("2026-01-01T09:00:00")]
    public void Compile_DslLiteralWithoutAnOffset_ReportsAnErrorThatNamesTheFix_Test(string text)
    {
        CompilationResult<Window> result = CreateCompiler().Compile($"atBetween(lower: \"{text}\", upper: {Instant})");

        AssertOffsetError(result);
    }

    /// <summary>Offset-less text in an array of date-times is rejected the same way.</summary>
    [Fact]
    public void Compile_DslArrayElementWithoutAnOffset_ReportsAnErrorThatNamesTheFix_Test()
    {
        CompilationResult<Window> result = CreateCompiler()
            .Compile("atAnyOf(values: [\"2026-01-01T00:00:00Z\", \"2026-01-02T09:00\"])");

        AssertOffsetError(result);
    }

    /// <summary>Offset-less text in a JSON rule literal is rejected.</summary>
    [Fact]
    public void CompileJson_LiteralWithoutAnOffset_ReportsAnErrorThatNamesTheFix_Test()
    {
        CompilationResult<Window> result = CreateCompiler()
            .CompileJson($$$"""{"predicate":"atBetween","args":{"lower":"2026-01-01T09:00","upper":{{{Instant}}}}}""");

        AssertOffsetError(result);
    }

    /// <summary>Offset-less text in a YAML rule literal is rejected.</summary>
    [Fact]
    public void CompileYaml_LiteralWithoutAnOffset_ReportsAnErrorThatNamesTheFix_Test()
    {
        CompilationResult<Window> result = CreateCompiler()
            .CompileYaml($"predicate: atBetween\nargs:\n  lower: \"2026-01-01T09:00\"\n  upper: {Instant}\n");

        AssertOffsetError(result);
    }

    /// <summary>Offset-less text passed to <see cref="RuleBuilder"/> is rejected when the rule compiles.</summary>
    [Fact]
    public void RuleBuilderCompile_LiteralWithoutAnOffset_ReportsAnErrorThatNamesTheFix_Test()
    {
        RuleBuilder rule = RuleBuilder.Predicate("atBetween", ("lower", "2026-01-01"), ("upper", "2026-02-01T00:00:00Z"));

        CompilationResult<Window> result = rule.Compile(CreateCompiler());

        AssertOffsetError(result);
    }

    /// <summary>Text that ends in <c>Z</c> or carries an offset compiles, with or without fractional seconds.</summary>
    [Theory]
    [InlineData("2026-01-01T09:00:00Z")]
    [InlineData("2026-01-01T09:00Z")]
    [InlineData("2026-01-01T09:00:00.5+02:00")]
    [InlineData("2026-01-01T09:00:00-04:30")]
    public void Compile_DslLiteralWithAnOffset_Compiles_Test(string text)
    {
        CompilationResult<Window> result = CreateCompiler().Compile($"atBetween(lower: \"{text}\", upper: {Instant})");

        Assert.True(result.Succeeded);
    }

    /// <summary>
    /// Offset-less text from a data source is not a date-time: the argument faults to <c>Unknown</c> and the fault message
    /// names the fix.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_VariableWithoutAnOffset_FaultsAsUnknownAndNamesTheFix_Test()
    {
        CompilationResult<Window> result = CreateCompiler()
            .Compile($"atBetween(lower: from(\"limits\", \"$.lower\"), upper: {Instant})");
        DataSources sources = new() { ["limits"] = new FixedSource(LiteralValue.OfString("2026-01-01T09:00")) };

        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new Window(DateTimeOffset.Parse("2026-01-15T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture)),
            dataSources: sources,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Fault fault = Assert.Single(decision.Faults);
        Assert.Contains("'Z'", fault.Exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Text with an offset from a data source resolves to the same instant under two different host zones.</summary>
    [Theory]
    [InlineData("UTC")]
    [InlineData("Pacific Standard Time")]
    public async Task EvaluateAsync_VariableWithAnOffset_GivesTheSameResultUnderAnyHostZone_Test(string zoneId)
    {
        using IDisposable zone = HostTimeZone.Use(zoneId);
        CompilationResult<Window> result = CreateCompiler()
            .Compile($"atBetween(lower: from(\"limits\", \"$.lower\"), upper: {Instant})");
        DataSources sources = new() { ["limits"] = new FixedSource(LiteralValue.OfString("2026-01-01T09:00:00+02:00")) };

        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new Window(DateTimeOffset.Parse("2026-01-15T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture)),
            dataSources: sources,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>
    /// The compile outcome for offset-less text is the same error under two different host zones; the host zone is never
    /// consulted.
    /// </summary>
    [Theory]
    [InlineData("UTC")]
    [InlineData("Pacific Standard Time")]
    public void Compile_LiteralWithoutAnOffset_IsRejectedUnderAnyHostZone_Test(string zoneId)
    {
        using IDisposable zone = HostTimeZone.Use(zoneId);

        CompilationResult<Window> result = CreateCompiler()
            .Compile($"atBetween(lower: \"2026-01-01T09:00\", upper: {Instant})");

        AssertOffsetError(result);
    }

    private static void AssertOffsetError(CompilationResult<Window> result)
    {
        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.ArgumentTypeMismatch, diagnostic.Code);
        Assert.Contains("'Z'", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("offset", diagnostic.Message, StringComparison.Ordinal);
    }

    private static RuleCompiler<Window> CreateCompiler()
    {
        PredicateRegistryBuilder<Window> builder = PredicateRegistry<Window>.CreateBuilder();
        (PredicateSchema schema, Func<Window, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            DateTimePredicates.Between<Window>("atBetween", w => w.At);
        builder.Add(schema, evaluate);
        PredicateSchema anyOf = new(
            "atAnyOf",
            "At any of",
            "Test predicate with a date-time array.",
            [new PredicateArgumentSchema("values", "Instants.", LiteralKind.DateTimeOffsetArray)]
        );
        builder.Add(anyOf, (_, _, _) => ValueTask.FromResult(TruthValue.True));
        return new RuleCompiler<Window>(builder.Build(), new CompilerOptions(DataSources: ["limits"]));
    }

    private sealed record Window(DateTimeOffset? At);

    /// <summary>A data source that answers every query with one value.</summary>
    private sealed class FixedSource(LiteralValue value) : IDataSource
    {
        public ValueTask<DataQueryResult> QueryAsync(string query, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(DataQueryResult.Success([value]));
        }

        public ValueTask<DataScopeResult> ScopeAsync(string query, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
