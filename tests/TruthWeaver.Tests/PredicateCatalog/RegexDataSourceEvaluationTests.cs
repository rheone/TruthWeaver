namespace TruthWeaver.Tests.PredicateCatalog;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Predicates;
using TruthWeaver.Registry;
using TruthWeaver.Testing;

/// <summary>
/// The regular-expression predicates check a <c>pattern</c> or <c>flags</c> value that a data source supplies at
/// evaluation, before they read the selected string. A bad value is a fault even when the selection is null.
/// </summary>
public sealed class RegexDataSourceEvaluationTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    /// <summary>
    /// A bad <c>pattern</c> or <c>flags</c> value from <c>from(...)</c>, with a null selected string, makes the decision
    /// <c>Unknown</c> with one fault that holds the argument exception.
    /// </summary>
    /// <param name="predicate">The predicate name, <c>isMatch</c> or <c>isNotMatch</c>.</param>
    /// <param name="pattern">The pattern the data source returns.</param>
    /// <param name="flags">The flags the data source returns.</param>
    [Theory]
    [InlineData("isMatch", "(", "")]
    [InlineData("isMatch", "a", "q")]
    [InlineData("isNotMatch", "(", "")]
    [InlineData("isNotMatch", "a", "ii")]
    public async Task EvaluateAsync_BadDataSourcePatternOrFlagsWithNullSelection_IsUnknownWithAFault_Test(
        string predicate,
        string pattern,
        string flags
    )
    {
        CompiledRule<Text> rule = Compile(
            $"{predicate}(pattern: from(\"config\", \"$.pattern\"), flags: from(\"config\", \"$.flags\"))"
        );
        FakeDataSource source = new FakeDataSource().With("$.pattern", pattern).With("$.flags", flags);
        DataSources sources = new() { ["config"] = source };

        Decision decision = await rule.EvaluateAsync(new Text(null), dataSources: sources, cancellationToken: Token);

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Fault fault = Assert.Single(decision.Faults);
        Assert.IsType<ArgumentException>(fault.Exception, exactMatch: false);
    }

    /// <summary>A good <c>pattern</c> and <c>flags</c> from <c>from(...)</c> with a null selected string is <c>Unknown</c> with no fault.</summary>
    [Fact]
    public async Task EvaluateAsync_GoodDataSourcePatternWithNullSelection_IsUnknownWithoutAFault_Test()
    {
        CompiledRule<Text> rule = Compile(
            "isMatch(pattern: from(\"config\", \"$.pattern\"), flags: from(\"config\", \"$.flags\"))"
        );
        FakeDataSource source = new FakeDataSource().With("$.pattern", "^a").With("$.flags", "i");
        DataSources sources = new() { ["config"] = source };

        Decision decision = await rule.EvaluateAsync(new Text(null), dataSources: sources, cancellationToken: Token);

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.Empty(decision.Faults);
    }

    private static CompiledRule<Text> Compile(string ruleText)
    {
        PredicateRegistryBuilder<Text> builder = PredicateRegistry<Text>.CreateBuilder();
        Register(builder, RegexPredicates.IsMatch<Text>("isMatch", c => c.Value));
        Register(builder, RegexPredicates.IsNotMatch<Text>("isNotMatch", c => c.Value));

        DataSourceDeclarations declarations = [];
        declarations.Add("config");
        RuleCompiler<Text> compiler = new(builder.Build(), new CompilerOptions(DataSources: declarations));
        CompilationResult<Text> result = compiler.Compile(ruleText);
        return result.CompiledRule
            ?? throw new InvalidOperationException($"Test rule did not compile: {result.FormatDiagnostics(ruleText)}");
    }

    private static void Register(
        PredicateRegistryBuilder<Text> builder,
        (PredicateSchema Schema, Func<Text, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate) predicate
    )
    {
        builder.Add(predicate.Schema, predicate.Evaluate);
    }

    private sealed record Text(string? Value);
}
