namespace TruthWeaver.Testing;

using System.Diagnostics;
using System.Globalization;
using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;

/// <summary>Assertions that a rewrite of a compiled rule keeps its meaning, for built-in rewrites and for your own.</summary>
[StackTraceHidden]
public static class RewriteAssertions
{
    /// <summary>
    /// Asserts that <paramref name="rewrite"/>, a rewrite that returns a <see cref="CompilationResult{TContext}"/>, is sound
    /// on <paramref name="rule"/>. This is the overload for the size-capped rewrites (<c>ExpandToPrimitives</c>,
    /// <c>ExpandToNand</c>, <c>ExpandToNor</c>, <c>ToNnf</c>, <c>ToCnf</c> and <c>ToDnf</c>), so <c>r =&gt; r.ToCnf()</c> needs
    /// no <c>.GetRuleOrThrow()</c>. A result that has no rule or has an error diagnostic, such as a <c>TRE0016</c> refusal,
    /// fails the assertion before the equivalence check runs. Warnings, such as <c>TRE0031</c>, do not.
    /// </summary>
    /// <typeparam name="TContext">The application context type of the rule.</typeparam>
    /// <param name="rule">The rule to rewrite.</param>
    /// <param name="rewrite">The rewrite, for example <c>r =&gt; r.ToCnf()</c>.</param>
    /// <param name="expectations">
    /// The extra checks: <see cref="RewriteExpectations.NeverLarger"/> and <see cref="RewriteExpectations.Idempotent"/>.
    /// </param>
    /// <param name="maxAnalysisTerms">
    /// The most distinct terms, counted across both rules, the equivalence check decides. It controls only that cap. Raise
    /// it to compare rules with more terms. <see langword="null"/> uses the <see cref="CompilerOptions.MaxAnalysisTerms"/> default.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> or <paramref name="rewrite"/> is <see langword="null"/>.</exception>
    /// <exception cref="DecisionAssertionException">
    /// The rewrite returned no rule or an error diagnostic, or a check failed. The message names the check and shows the
    /// diagnostics, the counter-example or the two sizes. An unproven claim never passes.
    /// </exception>
    public static void AssertSound<TContext>(
        CompiledRule<TContext> rule,
        Func<CompiledRule<TContext>, CompilationResult<TContext>> rewrite,
        RewriteExpectations expectations = RewriteExpectations.None,
        int? maxAnalysisTerms = null
    )
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(rewrite);

        // Unwrapping inside the adapter also covers the second pass of the Idempotent check.
        AssertSound(
            rule,
            input =>
            {
                CompilationResult<TContext> result = rewrite(input);
                if (result.CompiledRule is null || result.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                {
                    throw new DecisionAssertionException(
                        $"Rewrite check 'compiles' failed: the rewrite of '{input.CanonicalText}' returned no usable rule. {result.FormatDiagnostics()}"
                    );
                }

                return result.CompiledRule;
            },
            expectations,
            maxAnalysisTerms
        );
    }

    /// <summary>
    /// Asserts that <paramref name="rewrite"/> is sound on <paramref name="rule"/>. The rewritten rule must be K3
    /// equivalent to the original, as <see cref="RuleEquivalence.Compare{TContext}"/> decides. The checks in
    /// <paramref name="expectations"/> run after that.
    /// </summary>
    /// <typeparam name="TContext">The application context type of the rule.</typeparam>
    /// <param name="rule">The rule to rewrite.</param>
    /// <param name="rewrite">The rewrite, for example <c>r =&gt; r.Simplify()</c>.</param>
    /// <param name="expectations">
    /// The extra checks: <see cref="RewriteExpectations.NeverLarger"/> and <see cref="RewriteExpectations.Idempotent"/>.
    /// </param>
    /// <param name="maxAnalysisTerms">
    /// The most distinct terms, counted across both rules, the equivalence check decides. It controls only that cap. Raise
    /// it to compare rules with more terms. <see langword="null"/> uses the <see cref="CompilerOptions.MaxAnalysisTerms"/> default.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> or <paramref name="rewrite"/> is <see langword="null"/>.</exception>
    /// <exception cref="DecisionAssertionException">
    /// A check failed. The message names the check and shows the counter-example or the two sizes. The equivalence check
    /// can also be inconclusive when the rules have more distinct terms than <paramref name="maxAnalysisTerms"/>.
    /// An unproven claim never passes.
    /// </exception>
    public static void AssertSound<TContext>(
        CompiledRule<TContext> rule,
        Func<CompiledRule<TContext>, CompiledRule<TContext>> rewrite,
        RewriteExpectations expectations = RewriteExpectations.None,
        int? maxAnalysisTerms = null
    )
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(rewrite);

        CompiledRule<TContext> rewritten = rewrite(rule);
        RuleEquivalenceResult result = RuleEquivalence.Compare(rule, rewritten, maxAnalysisTerms);
        switch (result.Outcome)
        {
            case RuleEquivalenceOutcome.Equivalent:
                break;
            case RuleEquivalenceOutcome.NotEquivalent:
                throw new DecisionAssertionException(
                    $"Rewrite check 'equivalence' failed: '{rule.CanonicalText}' became '{rewritten.CanonicalText}', which is not equivalent. With {RuleAssertions.FormatCounterExample(result)}, they differ."
                );
            default:
                throw new DecisionAssertionException(
                    $"Rewrite check 'equivalence' is inconclusive: {result.Reason} Raise the maxAnalysisTerms argument or shrink the rule."
                );
        }

        if (expectations.HasFlag(RewriteExpectations.NeverLarger) && rewritten.Metrics.NodeCount > rule.Metrics.NodeCount)
        {
            throw new DecisionAssertionException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Rewrite check 'never larger' failed: the rule has {rule.Metrics.NodeCount} nodes and the rewritten rule has {rewritten.Metrics.NodeCount} nodes."
                )
            );
        }

        if (expectations.HasFlag(RewriteExpectations.Idempotent))
        {
            CompiledRule<TContext> again = rewrite(rewritten);
            if (again.CanonicalText != rewritten.CanonicalText)
            {
                throw new DecisionAssertionException(
                    $"Rewrite check 'idempotent' failed: rewriting '{rewritten.CanonicalText}' again gives '{again.CanonicalText}'."
                );
            }
        }
    }
}
