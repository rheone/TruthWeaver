namespace TruthWeaver.Tests.PredicateCatalog;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Predicates;
using TruthWeaver.Registry;

/// <summary>
/// The fixed-offset calendar predicates check their literal arguments at compile time: a bad offset, a time zone name or
/// a bad window is a <c>TRE0026</c> error at the call, and the rule does not compile.
/// </summary>
public sealed class FixedOffsetDiagnosticTests
{
    /// <summary>Each invalid literal argument gives one <c>TRE0026</c> error at the whole call, and no rule.</summary>
    [Theory]
    [InlineData("onDay(days: [\"Monday\"], offset: \"+5:30\")")]
    [InlineData("onDay(days: [\"Monday\"], offset: \"Europe/Paris\")")]
    [InlineData("onDay(days: [\"Someday\"], offset: \"Z\")")]
    [InlineData("inMonth(months: [13], offset: \"Z\")")]
    [InlineData("inWindow(start: \"09:00\", end: \"09:00\", offset: \"Z\")")]
    [InlineData("inWindow(start: \"09:00\", end: \"17:00\", duration: \"PT8H\", offset: \"Z\")")]
    [InlineData("inWindow(start: \"09:00\", offset: \"Z\")")]
    [InlineData("inWindow(start: \"09:00\", duration: \"PT24H\", offset: \"Z\")")]
    public void Compile_InvalidLiteralArgument_ReportsInvalidArgumentValueAtTheCall_Test(string rule)
    {
        CompilationResult<Clock> result = CreateCompiler().Compile(rule);

        Assert.False(result.Succeeded);
        Assert.Null(result.CompiledRule);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.InvalidArgumentValue, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(new SourceSpan(0, rule.Length), diagnostic.Span);
    }

    /// <summary>A time zone name is rejected with a message that names it and says time zone names are not supported.</summary>
    [Fact]
    public void Compile_TimeZoneName_ExplainsThatNamesAreNotSupported_Test()
    {
        CompilationResult<Clock> result = CreateCompiler().Compile("inMonth(months: [1], offset: \"Europe/Paris\")");

        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Contains("'Europe/Paris' is a time zone name", diagnostic.Message, StringComparison.Ordinal);
        Assert.Contains("not supported", diagnostic.Message, StringComparison.Ordinal);
    }

    /// <summary>Valid calls compile, including the duration form and the edge flags.</summary>
    [Theory]
    [InlineData("onDay(days: [\"Saturday\", \"sunday\"], offset: \"-03:00\")")]
    [InlineData("inMonth(months: [1, 12], offset: \"+05:30\")")]
    [InlineData("inWindow(start: \"22:00\", end: \"06:00\", offset: \"Z\")")]
    [InlineData("inWindow(start: \"09:00\", duration: \"PT1H30M\", includeEnd: true, offset: \"Z\")")]
    public void Compile_ValidArguments_Compiles_Test(string rule)
    {
        CompilationResult<Clock> result = CreateCompiler().Compile(rule);

        Assert.True(result.Succeeded);
        Assert.Empty(result.Diagnostics);
    }

    private static RuleCompiler<Clock> CreateCompiler()
    {
        PredicateRegistryBuilder<Clock> builder = PredicateRegistry<Clock>.CreateBuilder();
        Register(builder, DateTimePredicates.OnDayOfWeek<Clock>("onDay", c => c.At));
        Register(builder, DateTimePredicates.InMonth<Clock>("inMonth", c => c.At));
        Register(builder, DateTimePredicates.InTimeWindow<Clock>("inWindow", c => c.At));
        return new RuleCompiler<Clock>(builder.Build());
    }

    private static void Register(
        PredicateRegistryBuilder<Clock> builder,
        (PredicateSchema Schema, Func<Clock, PredicateArguments, CancellationToken, ValueTask<TruthValue>> Evaluate) predicate
    )
    {
        builder.Add(predicate.Schema, predicate.Evaluate);
    }

    private sealed record Clock(DateTimeOffset? At);
}
