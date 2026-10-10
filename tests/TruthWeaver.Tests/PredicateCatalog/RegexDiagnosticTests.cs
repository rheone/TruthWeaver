namespace TruthWeaver.Tests.PredicateCatalog;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Predicates;
using TruthWeaver.Registry;

/// <summary>
/// The regular-expression predicates check a literal pattern at compile time: an invalid pattern is a <c>TRE0026</c>
/// error at the call, and the rule does not compile.
/// </summary>
public sealed class RegexDiagnosticTests
{
    /// <summary>An invalid literal pattern gives one <c>TRE0026</c> error at the whole call, and no rule.</summary>
    [Theory]
    [InlineData("matches(pattern: \"(\")")]
    [InlineData("notMatches(pattern: \"[a-\")")]
    [InlineData("matchesIgnoreCase(pattern: \"(\")")]
    [InlineData("notMatchesIgnoreCase(pattern: \"*a\")")]
    public void Compile_InvalidLiteralPattern_ReportsInvalidArgumentValueAtTheCall_Test(string rule)
    {
        CompilationResult<Text> result = CreateCompiler().Compile(rule);

        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledRule);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.InvalidArgumentValue, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(new SourceSpan(0, rule.Length), diagnostic.Span);
        Assert.Contains("'pattern'", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>A valid literal pattern compiles.</summary>
    [Theory]
    [InlineData("matches(pattern: \"^[A-Z]{2}[0-9]{4}$\")")]
    [InlineData("notMatches(pattern: \"(?i)^ab\")")]
    [InlineData("matchesIgnoreCase(pattern: \"^ab\")")]
    [InlineData("notMatchesIgnoreCase(pattern: \"^ab\")")]
    public void Compile_ValidLiteralPattern_Compiles_Test(string rule)
    {
        CompilationResult<Text> result = CreateCompiler().Compile(rule);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    private static RuleCompiler<Text> CreateCompiler()
    {
        PredicateRegistryBuilder<Text> builder = PredicateRegistry<Text>.CreateBuilder();
        Register(builder, RegexPredicates.Matches<Text>("matches", c => c.Value));
        Register(builder, RegexPredicates.NotMatches<Text>("notMatches", c => c.Value));
        Register(builder, RegexPredicates.MatchesIgnoreCase<Text>("matchesIgnoreCase", c => c.Value));
        Register(builder, RegexPredicates.NotMatchesIgnoreCase<Text>("notMatchesIgnoreCase", c => c.Value));
        return new RuleCompiler<Text>(builder.Build());
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
