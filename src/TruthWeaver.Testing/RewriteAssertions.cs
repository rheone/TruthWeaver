namespace TruthWeaver.Testing;

using System.Diagnostics;
using System.Globalization;
using TruthWeaver.Analysis;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;

/// <summary>Assertions that a rewrite of a compiled rule keeps its meaning, for built-in rewrites and for your own.</summary>
[StackTraceHidden]
public static class RewriteAssertions
{
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
    /// <param name="options">
    /// The bounds for the equivalence check; only <see cref="CompilerOptions.MaxAnalysisTerms"/> is used. Defaults to
    /// <see cref="CompilerOptions"/> defaults when <see langword="null"/>.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> or <paramref name="rewrite"/> is <see langword="null"/>.</exception>
    /// <exception cref="DecisionAssertionException">
    /// A check failed. The message names the check and shows the counter-example or the two sizes. The equivalence check
    /// can also be inconclusive when the rules have more distinct terms than <see cref="CompilerOptions.MaxAnalysisTerms"/>.
    /// An unproven claim never passes.
    /// </exception>
    public static void AssertSound<TContext>(
        CompiledRule<TContext> rule,
        Func<CompiledRule<TContext>, CompiledRule<TContext>> rewrite,
        RewriteExpectations expectations = RewriteExpectations.None,
        CompilerOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(rewrite);

        CompiledRule<TContext> rewritten = rewrite(rule);
        RuleEquivalenceResult result = RuleEquivalence.Compare(rule, rewritten, options);
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
                    $"Rewrite check 'equivalence' is inconclusive: {result.Reason} Raise CompilerOptions.MaxAnalysisTerms or shrink the rule."
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
