namespace TruthWeaver.Analysis;

using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;

/// <summary>
/// Decides which of the nine pairs of Strong K3 values two peer rules can produce for the same assignment of their
/// terms, with an example assignment for each reachable pair (ADR-0009: joint outcome, overlap and conflict). Unlike
/// <see cref="RuleImplication"/>, which reads one rule as a new version of the other, this reads the two rules as
/// peers: a permission rule per action, a feature flag per audience. The check uses the same dual-rail BDD as the
/// analyzer, <see cref="RuleEquivalence"/> and <see cref="RuleImplication"/>, so it is exact (not sampled) and does
/// not enumerate truth tables.
/// </summary>
/// <remarks>
/// An <em>unreachable</em> pair is a proof for every real application context, provided the predicate-author
/// contract holds (the same term answers the same way within one evaluation): every real context produces some
/// assignment, so no context can reach a pair that no assignment reaches. A <em>reachable</em> pair, with its
/// example, is a candidate only: terms are opaque and independent (<c>CONTEXT.md</c>, Term identity), so the example
/// assignment can be impossible in the application (<c>ageAtLeast(n: 21) = True</c> with
/// <c>ageAtLeast(n: 18) = False</c>). The checks do not look at evaluation order, short-circuiting or faults.
/// </remarks>
public static class RuleOverlap
{
    /// <summary>
    /// Decides which of the nine pairs <c>(first(σ), second(σ))</c> some assignment <c>σ</c> of the terms of
    /// <paramref name="first"/> and <paramref name="second"/> can produce.
    /// </summary>
    /// <typeparam name="TContext">The application context type both rules were compiled for.</typeparam>
    /// <param name="first">The first rule.</param>
    /// <param name="second">The second rule.</param>
    /// <param name="maxAnalysisTerms">
    /// The most distinct terms, counted across both rules, the check decides. <see langword="null"/> uses the
    /// <see cref="CompilerOptions.MaxAnalysisTerms"/> default.
    /// </param>
    /// <returns>
    /// The reachable pairs with an example each, or <see cref="RuleOverlapOutcome.Undecided"/> with a reason when the
    /// rules have more distinct terms than <paramref name="maxAnalysisTerms"/> or their shared BDD would exceed the
    /// internal node budget. It never throws for a normal rule.
    /// </returns>
    /// <exception cref="ArgumentNullException">A rule is <see langword="null"/>.</exception>
    public static RuleOverlapResult Compare<TContext>(
        CompiledRule<TContext> first,
        CompiledRule<TContext> second,
        int? maxAnalysisTerms = null
    )
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        AnalysisAttempt<List<JointOutcome>> attempt = AnalysisVerdicts.Run(
            [first.Root, second.Root],
            maxAnalysisTerms,
            () => ReachableOutcomes(first.Root, second.Root)
        );
        return attempt.IsDecided
            ? new RuleOverlapResult(RuleOverlapOutcome.Decided, attempt.Value!, null)
            : new RuleOverlapResult(RuleOverlapOutcome.Undecided, [], attempt.UndecidedReason);
    }

    /// <summary>Gets the reachable joint outcomes of two rule roots, each example keyed by the printed term.</summary>
    private static List<JointOutcome> ReachableOutcomes(Expression first, Expression second)
    {
        return
        [
            .. Analyzer
                .JointOutcomes(first, second)
                .Select(cell => new JointOutcome(cell.First, cell.Second, AnalysisVerdicts.Printed(cell.Example))),
        ];
    }
}
