namespace TruthWeaver.Analysis;

using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;

/// <summary>
/// Decides whether one compiled rule implies another, and whether a changed rule is looser or tighter than the rule it
/// replaces. An assignment of <c>True</c>, <c>False</c> or <c>Unknown</c> to the terms satisfies a rule when the rule is
/// <c>True</c> for it, as <see cref="Decision.IsSatisfied"/> reads a decision. The checks use the same dual-rail BDD as
/// the analyzer and <see cref="RuleEquivalence"/>, so they are exact (not sampled) and do not enumerate truth tables.
/// </summary>
/// <remarks>
/// The checks compare only where each rule is <c>True</c>. Two rules that are <c>True</c> for the same assignments are
/// <see cref="RuleRelation.Equivalent"/> here even when one is <c>False</c> where the other is <c>Unknown</c>; use
/// <see cref="RuleEquivalence"/> to compare every value. Terms are opaque and independent, as for
/// <see cref="RuleEquivalence"/>: two terms are the same variable only when they have the same <see cref="TermIdentity"/>.
/// The checks do not look at evaluation order, short-circuiting or faults.
/// </remarks>
public static class RuleImplication
{
    /// <summary>Decides whether every assignment of the terms that satisfies <paramref name="first"/> also satisfies <paramref name="second"/>.</summary>
    /// <typeparam name="TContext">The application context type both rules were compiled for.</typeparam>
    /// <param name="first">The rule that must imply the other.</param>
    /// <param name="second">The rule that must be implied.</param>
    /// <param name="maxAnalysisTerms">
    /// The most distinct terms, counted across both rules, the check decides. <see langword="null"/> uses the
    /// <see cref="CompilerOptions.MaxAnalysisTerms"/> default.
    /// </param>
    /// <returns>
    /// <see cref="RuleImplicationOutcome.Holds"/>, <see cref="RuleImplicationOutcome.DoesNotHold"/> with a counter-example,
    /// or <see cref="RuleImplicationOutcome.Undecided"/> when the rules have more distinct terms than
    /// <paramref name="maxAnalysisTerms"/> or their shared BDD would exceed the internal node budget. It never throws
    /// for a normal rule.
    /// </returns>
    /// <exception cref="ArgumentNullException">A rule is <see langword="null"/>.</exception>
    public static RuleImplicationResult Implies<TContext>(
        CompiledRule<TContext> first,
        CompiledRule<TContext> second,
        int? maxAnalysisTerms = null
    )
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        AnalysisAttempt<IReadOnlyDictionary<TermIdentity, TruthValue>> attempt = AnalysisVerdicts.Run(
            [first.Root, second.Root],
            maxAnalysisTerms,
            () => Analyzer.CompareSatisfaction(first.Root, second.Root).OnlyLeft
        );
        if (!attempt.IsDecided)
        {
            return new RuleImplicationResult(RuleImplicationOutcome.Undecided, null, attempt.UndecidedReason);
        }

        // A decided null value means no assignment satisfies only the first rule: the implication holds.
        return attempt.Value is null
            ? new RuleImplicationResult(RuleImplicationOutcome.Holds, null, null)
            : new RuleImplicationResult(RuleImplicationOutcome.DoesNotHold, AnalysisVerdicts.Printed(attempt.Value), null);
    }

    /// <summary>Decides how the satisfying assignments of <paramref name="after"/> relate to those of <paramref name="before"/>.</summary>
    /// <typeparam name="TContext">The application context type both rules were compiled for.</typeparam>
    /// <param name="before">The rule to compare from, such as the current version.</param>
    /// <param name="after">The rule to compare to, such as the proposed version.</param>
    /// <param name="maxAnalysisTerms">
    /// The most distinct terms, counted across both rules, the check decides. <see langword="null"/> uses the
    /// <see cref="CompilerOptions.MaxAnalysisTerms"/> default.
    /// </param>
    /// <returns>
    /// The relation of <paramref name="after"/> to <paramref name="before"/>, with an example assignment for each side that
    /// only one rule accepts, or <see cref="RuleRelation.Undecided"/> when the rules have more distinct terms than
    /// <paramref name="maxAnalysisTerms"/> or their shared BDD would exceed the internal node budget. It never throws
    /// for a normal rule.
    /// </returns>
    /// <exception cref="ArgumentNullException">A rule is <see langword="null"/>.</exception>
    public static RuleRelationResult Compare<TContext>(
        CompiledRule<TContext> before,
        CompiledRule<TContext> after,
        int? maxAnalysisTerms = null
    )
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        AnalysisAttempt<(
            IReadOnlyDictionary<TermIdentity, TruthValue>? OnlyLeft,
            IReadOnlyDictionary<TermIdentity, TruthValue>? OnlyRight,
            bool Shared
        )> attempt = AnalysisVerdicts.Run(
            [before.Root, after.Root],
            maxAnalysisTerms,
            () => Analyzer.CompareSatisfaction(before.Root, after.Root)
        );
        if (!attempt.IsDecided)
        {
            return new RuleRelationResult(RuleRelation.Undecided, null, null, attempt.UndecidedReason);
        }

        (
            IReadOnlyDictionary<TermIdentity, TruthValue>? onlyBefore,
            IReadOnlyDictionary<TermIdentity, TruthValue>? onlyAfter,
            bool shared
        ) = attempt.Value;

        // The subset checks come first, so a rule that is never True is tighter (or equivalent), not disjoint.
        RuleRelation relation = (onlyBefore, onlyAfter) switch
        {
            (null, null) => RuleRelation.Equivalent,
            (_, null) => RuleRelation.Tighter,
            (null, _) => RuleRelation.Looser,
            _ => shared ? RuleRelation.Overlapping : RuleRelation.Disjoint,
        };
        return new RuleRelationResult(
            relation,
            AnalysisVerdicts.Printed(onlyBefore),
            AnalysisVerdicts.Printed(onlyAfter),
            null
        );
    }
}
