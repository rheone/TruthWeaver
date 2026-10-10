namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// A repeat-until-stable rewrite that stops at its pass limit reports <c>TRE0033</c> with a rule that is still equivalent,
/// so "rewrite again changes nothing" is never claimed silently.
/// </summary>
public sealed class RewriteConvergenceTests
{
    /// <summary>A rule that needs a second pass to be confirmed stable gets the warning when the limit is one pass.</summary>
    [Fact]
    public void Canonicalize_PassLimitReachedBeforeFixedPoint_ReportsRewriteNotConvergedWarning_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("b AND a");

        CompilationResult<RuleTestContext> result = rule.CanonicalizeWithPassLimit(1);

        Diagnostic warning = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.RewriteNotConverged, warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.True(result.Succeeded);
        Assert.Equal("a AND b", result.Rule.CanonicalText);
    }

    /// <summary>The compression warning names its rewrite and keeps an equivalent rule.</summary>
    [Fact]
    public void CompressToDerived_PassLimitReachedBeforeFixedPoint_ReportsRewriteNotConvergedWarning_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("NOT a OR b");

        CompilationResult<RuleTestContext> result = rule.CompressToDerivedWithPassLimit(1);

        Diagnostic warning = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.RewriteNotConverged, warning.Code);
        Assert.Contains(nameof(CompiledRule<>.CompressToDerived), warning.Message);
        Assert.True(result.Succeeded);
    }

    /// <summary>The simplification warning names its rewrite and keeps an equivalent rule.</summary>
    [Fact]
    public void Simplify_PassLimitReachedBeforeFixedPoint_ReportsRewriteNotConvergedWarning_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND True");

        CompilationResult<RuleTestContext> result = rule.SimplifyWithPassLimit(1);

        Diagnostic warning = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCodes.RewriteNotConverged, warning.Code);
        Assert.Contains(nameof(CompiledRule<>.Simplify), warning.Message);
        Assert.True(result.Succeeded);
    }

    /// <summary>With the real pass limits, the rewrites reach a fixed point and report nothing.</summary>
    [Theory]
    [InlineData("b AND a")]
    [InlineData("NOT a OR b")]
    [InlineData("(a AND True) AND (a OR b)")]
    public void Rewrites_DefaultPassLimit_ReportNoDiagnostics_Test(string text)
    {
        CompiledRule<RuleTestContext> rule = Compile(text);

        Assert.Empty(rule.Canonicalize().Diagnostics);
        Assert.Empty(rule.CompressToDerived().Diagnostics);
        Assert.Empty(rule.Simplify().Diagnostics);
        Assert.Empty(rule.SimplifyWithSteps().Diagnostics);
    }

    private static CompiledRule<RuleTestContext> Compile(string text)
    {
        return K3Rule.TryCreate(text, 2)!.Compiled;
    }
}
