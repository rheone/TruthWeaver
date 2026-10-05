namespace TruthWeaver.Tests.PredicateCatalog;

using TruthWeaver.Abstractions;
using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Predicates;
using TruthWeaver.Registry;
using TruthWeaver.Yaml;

/// <summary>
/// Catalog rule 7: reversed literal bounds (<c>lower &gt; upper</c>) on the numeric and date-time <c>Between</c> and
/// <c>Outside</c> predicates are a compile-time <c>TRE0026</c> error at the predicate call, in every front end. Bounds that
/// are not literals are not checked at compile time and still fault at evaluation. The bounds are never swapped.
/// </summary>
public sealed class ReversedBoundsDiagnosticTests
{
    private const string EarlyInstant = "\"2026-01-01T00:00:00Z\"";
    private const string LateInstant = "\"2026-02-01T00:00:00Z\"";

    /// <summary>Reversed literal bounds on each range predicate give one error at the whole call, and no rule.</summary>
    [Theory]
    [InlineData("countBetween(lower: 20, upper: 10)", "20", "10")]
    [InlineData("countOutside(lower: 20, upper: 10)", "20", "10")]
    [InlineData("priceBetween(lower: 2.5, upper: 1.5)", "2.5", "1.5")]
    [InlineData("priceOutside(lower: 2.5, upper: 1.5)", "2.5", "1.5")]
    [InlineData("atBetween(lower: " + LateInstant + ", upper: " + EarlyInstant + ")", "2026-02-01", "2026-01-01")]
    [InlineData("atOutside(lower: " + LateInstant + ", upper: " + EarlyInstant + ")", "2026-02-01", "2026-01-01")]
    public void Compile_ReversedLiteralBounds_ReportsInvalidArgumentValueAtTheCall_Test(
        string rule,
        string lowerText,
        string upperText
    )
    {
        CompilationResult<Ranges> result = CreateCompiler().Compile(rule);

        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledRule);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.InvalidArgumentValue, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(new SourceSpan(0, rule.Length), diagnostic.Span);
        Assert.Contains("'lower' less than or equal to 'upper'", diagnostic.Expected, StringComparison.Ordinal);
        Assert.Contains(lowerText, diagnostic.Found, StringComparison.Ordinal);
        Assert.Contains(upperText, diagnostic.Found, StringComparison.Ordinal);
        Assert.NotNull(diagnostic.Suggestion);
        Assert.Contains("Swap", diagnostic.Suggestion.Text, StringComparison.Ordinal);
    }

    /// <summary>Equal bounds are a single-point range and ordered bounds are a normal range; both compile.</summary>
    [Theory]
    [InlineData("countBetween(lower: 10, upper: 10)")]
    [InlineData("countOutside(lower: 10, upper: 20)")]
    [InlineData("priceBetween(lower: 1.5, upper: 1.50)")]
    [InlineData("priceOutside(lower: 1.5, upper: 2.5)")]
    [InlineData("atBetween(lower: " + EarlyInstant + ", upper: " + EarlyInstant + ")")]
    [InlineData("atOutside(lower: " + EarlyInstant + ", upper: " + LateInstant + ")")]
    public void Compile_EqualOrOrderedLiteralBounds_Compiles_Test(string rule)
    {
        CompilationResult<Ranges> result = CreateCompiler().Compile(rule);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    /// <summary>The same reversed bounds in a JSON rule are reported at the predicate node by path and span.</summary>
    [Fact]
    public void CompileJson_ReversedLiteralBounds_PointsAtThePredicateNode_Test()
    {
        const string predicate = """{"predicate":"countBetween","args":{"lower":20,"upper":10}}""";
        string json =
            $$$"""{"op":"and","operands":[{"predicate":"countBetween","args":{"lower":1,"upper":2}},{{{predicate}}}]}""";

        CompilationResult<Ranges> result = CreateCompiler().CompileJson(json);

        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.InvalidArgumentValue, diagnostic.Code);
        Assert.Equal("$.operands[1]", diagnostic.Path);
        Assert.Equal(json.IndexOf(predicate, StringComparison.Ordinal), diagnostic.Span.Start);
    }

    /// <summary>The same reversed bounds in a YAML rule are reported at the predicate node by path.</summary>
    [Fact]
    public void CompileYaml_ReversedLiteralBounds_PointsAtThePredicateNode_Test()
    {
        const string yaml =
            "op: and\noperands:\n  - predicate: countBetween\n    args: { lower: 1, upper: 2 }\n"
            + "  - predicate: countBetween\n    args: { lower: 20, upper: 10 }\n";

        CompilationResult<Ranges> result = CreateCompiler().CompileYaml(yaml);

        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.InvalidArgumentValue, diagnostic.Code);
        Assert.Equal("$.operands[1]", diagnostic.Path);
    }

    /// <summary>A <see cref="RuleBuilder"/> rule with reversed literal bounds gets the same diagnostic as the text front ends.</summary>
    [Fact]
    public void RuleBuilderCompile_ReversedLiteralBounds_ReportsInvalidArgumentValue_Test()
    {
        RuleBuilder rule = RuleBuilder.Predicate("priceOutside", ("lower", 2.5m), ("upper", 1.5m));

        CompilationResult<Ranges> result = rule.Compile(CreateCompiler());

        Assert.False(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.InvalidArgumentValue, diagnostic.Code);
        Assert.Equal("$", diagnostic.Path);
    }

    /// <summary>
    /// A bound read from a data source is not a literal, so the rule compiles; a reversed value at evaluation still
    /// faults (<c>Unknown</c> plus an <see cref="ArgumentException"/> fault) and is never swapped.
    /// </summary>
    [Theory]
    [InlineData("countBetween")]
    [InlineData("countOutside")]
    public async Task EvaluateAsync_ReversedBoundFromADataSource_FaultsAsUnknown_Test(string predicate)
    {
        CompilationResult<Ranges> result = CreateCompiler()
            .Compile($"{predicate}(lower: from(\"limits\", \"$.lower\"), upper: 10)");
        DataSources sources = new() { ["limits"] = new FixedSource(LiteralValue.OfInt64(20)) };

        Decision decision = await result.CompiledRule!.EvaluateAsync(
            new Ranges(15, null, null),
            dataSources: sources,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.True(result.Succeeded);
        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.IsType<ArgumentException>(Assert.Single(decision.Faults).Exception);
    }

    /// <summary>
    /// A schema's argument validator sees only the literal arguments and each problem it returns becomes one
    /// <c>TRE0026</c> error with the problem's message, expected, found and suggestion text.
    /// </summary>
    [Fact]
    public void Compile_SchemaValidatorProblem_BecomesADiagnosticWithItsText_Test()
    {
        IReadOnlyList<string>? seen = null;
        PredicateSchema schema = new(
            "even",
            "Even",
            "Test predicate.",
            [
                new PredicateArgumentSchema("n", "A number.", LiteralKind.Int64),
                new PredicateArgumentSchema("m", "Another number.", LiteralKind.Int64),
            ]
        )
        {
            ArgumentValidator = args =>
            {
                List<string> present = [];
                foreach (string name in (string[])["n", "m"])
                {
                    if (args.TryGetRaw(name, out _))
                    {
                        present.Add(name);
                    }
                }

                seen = present;
                return [new PredicateArgumentProblem("'n' must be even.", "an even 'n'", "'n' is 3", "Use 2 or 4.")];
            },
        };
        PredicateRegistry<Ranges> registry = PredicateRegistry<Ranges>
            .CreateBuilder()
            .Add(schema, (_, _, _) => ValueTask.FromResult(TruthValue.True))
            .Build();
        RuleCompiler<Ranges> compiler = new(registry, new CompilerOptions(DataSources: ["limits"]));

        CompilationResult<Ranges> result = compiler.Compile("even(n: 3, m: from(\"limits\", \"$.m\"))");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.InvalidArgumentValue, diagnostic.Code);
        Assert.Contains("'n' must be even.", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal("an even 'n'", diagnostic.Expected);
        Assert.Equal("'n' is 3", diagnostic.Found);
        Assert.Equal("Use 2 or 4.", diagnostic.Suggestion?.Text);
        Assert.Equal(["n"], seen);
    }

    private static RuleCompiler<Ranges> CreateCompiler()
    {
        PredicateRegistryBuilder<Ranges> builder = PredicateRegistry<Ranges>.CreateBuilder();
        Register(builder, NumericPredicates.Between<Ranges>("countBetween", c => c.Count));
        Register(builder, NumericPredicates.Outside<Ranges>("countOutside", c => c.Count));
        Register(builder, NumericPredicates.Between<Ranges>("priceBetween", c => c.Price));
        Register(builder, NumericPredicates.Outside<Ranges>("priceOutside", c => c.Price));
        Register(builder, DateTimePredicates.Between<Ranges>("atBetween", c => c.At));
        Register(builder, DateTimePredicates.Outside<Ranges>("atOutside", c => c.At));
        return new RuleCompiler<Ranges>(builder.Build(), new CompilerOptions(DataSources: ["limits"]));
    }

    private static void Register(
        PredicateRegistryBuilder<Ranges> builder,
        (PredicateSchema Schema, Func<Ranges, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate) predicate
    )
    {
        builder.Add(predicate.Schema, predicate.Evaluate);
    }

    private sealed record Ranges(long? Count, decimal? Price, DateTimeOffset? At);

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
