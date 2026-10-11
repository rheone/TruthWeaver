namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;

/// <summary>
/// A predicate whose schema carries a <see cref="PredicateDeprecation"/> compiles with one <c>TRE0027</c> warning per use,
/// at the call, naming the replacement. A predicate without the marker produces no such diagnostic.
/// </summary>
public sealed class PredicateDeprecationTests
{
    /// <summary>One use of a deprecated predicate gives a warning at the call that names the replacement, and the rule compiles.</summary>
    [Fact]
    public void Compile_DeprecatedPredicate_WarnsAtTheCallAndStillCompiles_Test()
    {
        const string rule = "old(n: 1)";
        RuleCompiler<object> compiler = CreateCompiler(
            new PredicateDeprecation("fresh", "Use fresh instead; old will be removed.")
        );

        CompilationResult<object> result = compiler.Compile(rule);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.CompiledRule);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.DeprecatedPredicate, diagnostic.Code);
        Assert.Equal("TRE0027", diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(new SourceSpan(0, rule.Length), diagnostic.Span);
        Assert.Contains("'old'", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("fresh", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("Use fresh instead", diagnostic.Message, StringComparison.Ordinal);
        Assert.NotNull(diagnostic.Suggestion);
        Assert.Contains("fresh", diagnostic.Suggestion.Text, StringComparison.Ordinal);
    }

    /// <summary>Each use of a deprecated predicate gets its own warning.</summary>
    [Fact]
    public void Compile_DeprecatedPredicateUsedTwice_WarnsOncePerUse_Test()
    {
        RuleCompiler<object> compiler = CreateCompiler(new PredicateDeprecation("fresh", null));

        CompilationResult<object> result = compiler.Compile("old(n: 1) AND old(n: 2)");

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Diagnostics.Count(d => d.Code == DiagnosticCodes.DeprecatedPredicate));
    }

    /// <summary>A deprecation with no replacement still warns and offers no suggestion.</summary>
    [Fact]
    public void Compile_DeprecatedPredicateWithoutReplacement_WarnsWithoutSuggestion_Test()
    {
        RuleCompiler<object> compiler = CreateCompiler(new PredicateDeprecation(null, null));

        CompilationResult<object> result = compiler.Compile("old(n: 1)");

        Assert.True(result.Succeeded);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.DeprecatedPredicate, diagnostic.Code);
        Assert.Null(diagnostic.Suggestion);
    }

    /// <summary>A predicate without a deprecation marker produces no diagnostic.</summary>
    [Fact]
    public void Compile_PredicateWithoutDeprecation_ProducesNoDiagnostic_Test()
    {
        RuleCompiler<object> compiler = CreateCompiler(null);

        CompilationResult<object> result = compiler.Compile("old(n: 1)");

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    private static RuleCompiler<object> CreateCompiler(PredicateDeprecation? deprecation)
    {
        PredicateSchema schema = new(
            "old",
            "Old",
            "A test predicate.",
            [new PredicateArgumentSchema("n", "A number.", LiteralKind.Int64)]
        )
        {
            Deprecation = deprecation,
        };
        PredicateRegistry<object> registry = PredicateRegistry<object>
            .CreateBuilder()
            .Add(schema, (_, _, _) => ValueTask.FromResult(TruthValue.True))
            .Build();
        return new RuleCompiler<object>(registry);
    }
}
