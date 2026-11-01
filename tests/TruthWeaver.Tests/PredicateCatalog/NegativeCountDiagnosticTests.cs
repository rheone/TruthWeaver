namespace TruthWeaver.Tests.PredicateCatalog;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Predicates;
using TruthWeaver.Registry;

/// <summary>
/// A <c>HasCount</c> predicate checks its literal count at compile time: a negative count is a <c>TRE0026</c> error at
/// the call, and the rule does not compile.
/// </summary>
public sealed class NegativeCountDiagnosticTests
{
    /// <summary>A negative literal count gives one <c>TRE0026</c> error at the whole call, and no rule.</summary>
    [Fact]
    public void Compile_NegativeLiteralCount_ReportsInvalidArgumentValueAtTheCall_Test()
    {
        const string rule = "hasCount(count: -1)";

        CompilationResult<Basket> result = CreateCompiler().Compile(rule);

        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledRule);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.InvalidArgumentValue, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(new SourceSpan(0, rule.Length), diagnostic.Span);
        Assert.Contains("-1", diagnostic.Found, StringComparison.Ordinal);
    }

    /// <summary>A count of zero compiles, because an empty collection has zero elements.</summary>
    [Fact]
    public void Compile_ZeroLiteralCount_Compiles_Test()
    {
        CompilationResult<Basket> result = CreateCompiler().Compile("hasCount(count: 0)");

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    private static RuleCompiler<Basket> CreateCompiler()
    {
        PredicateRegistryBuilder<Basket> builder = PredicateRegistry<Basket>.CreateBuilder();
        (PredicateSchema schema, Func<Basket, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            CollectionPredicates.HasCountEqualTo<Basket>("hasCount", b => b.Items);
        builder.Add(schema, evaluate);
        return new RuleCompiler<Basket>(builder.Build());
    }

    private sealed record Basket(IReadOnlyCollection<string>? Items);
}
