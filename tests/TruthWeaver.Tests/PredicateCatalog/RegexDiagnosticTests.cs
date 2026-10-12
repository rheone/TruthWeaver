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
    [InlineData("isMatch(pattern: \"(\")")]
    [InlineData("isNotMatch(pattern: \"[a-\")")]
    [InlineData("isMatch(pattern: \"(\", flags: \"i\")")]
    [InlineData("isNotMatch(pattern: \"*a\", flags: \"ms\")")]
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

    /// <summary>An unknown or repeated literal flag letter gives one <c>TRE0026</c> error at the whole call, naming <c>flags</c>.</summary>
    [Theory]
    [InlineData("isMatch(pattern: \"a\", flags: \"q\")")]
    [InlineData("isMatch(pattern: \"a\", flags: \"I\")")]
    [InlineData("isNotMatch(pattern: \"a\", flags: \"ii\")")]
    [InlineData("isNotMatch(pattern: \"a\", flags: \"imi\")")]
    public void Compile_InvalidLiteralFlags_ReportsInvalidArgumentValueAtTheCall_Test(string rule)
    {
        CompilationResult<Text> result = CreateCompiler().Compile(rule);

        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledRule);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.InvalidArgumentValue, diagnostic.Code);
        Assert.Equal(new SourceSpan(0, rule.Length), diagnostic.Span);
        Assert.Contains("'flags'", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>Invalid flags and an invalid pattern together give one error, for the flags, because the options are unknown.</summary>
    [Fact]
    public void Compile_InvalidFlagsAndInvalidPattern_ReportsOnlyTheFlags_Test()
    {
        CompilationResult<Text> result = CreateCompiler().Compile("isMatch(pattern: \"(\", flags: \"q\")");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Contains("'flags'", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>A valid literal pattern compiles.</summary>
    [Theory]
    [InlineData("isMatch(pattern: \"^[A-Z]{2}[0-9]{4}$\")")]
    [InlineData("isNotMatch(pattern: \"(?i)^ab\")")]
    [InlineData("isMatch(pattern: \"^ab\", flags: \"i\")")]
    [InlineData("isNotMatch(pattern: \"^ab\", flags: \"imsnx\")")]
    [InlineData("isMatch(pattern: \"^ab\", flags: \"\")")]
    public void Compile_ValidLiteralPattern_Compiles_Test(string rule)
    {
        CompilationResult<Text> result = CreateCompiler().Compile(rule);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    private static RuleCompiler<Text> CreateCompiler()
    {
        PredicateRegistryBuilder<Text> builder = PredicateRegistry<Text>.CreateBuilder();
        Register(builder, RegexPredicates.IsMatch<Text>("isMatch", c => c.Value));
        Register(builder, RegexPredicates.IsNotMatch<Text>("isNotMatch", c => c.Value));
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
