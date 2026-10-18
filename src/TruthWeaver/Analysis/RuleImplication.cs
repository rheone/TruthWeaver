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

        string? reason = CapReason(first, second, maxAnalysisTerms);
        if (reason is not null)
        {
            return new RuleImplicationResult(RuleImplicationOutcome.Undecided, null, reason);
        }

        IReadOnlyDictionary<TermIdentity, TruthValue>? onlyFirst;
        try
        {
            onlyFirst = Analyzer.CompareSatisfaction(first.Root, second.Root).OnlyLeft;
        }
        catch (BddNodeBudgetExceededException exception)
        {
            return new RuleImplicationResult(RuleImplicationOutcome.Undecided, null, exception.Message);
        }

        return onlyFirst is null
            ? new RuleImplicationResult(RuleImplicationOutcome.Holds, null, null)
            : new RuleImplicationResult(RuleImplicationOutcome.DoesNotHold, Printed(onlyFirst), null);
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

        string? reason = CapReason(before, after, maxAnalysisTerms);
        if (reason is not null)
        {
            return new RuleRelationResult(RuleRelation.Undecided, null, null, reason);
        }

        IReadOnlyDictionary<TermIdentity, TruthValue>? onlyBefore;
        IReadOnlyDictionary<TermIdentity, TruthValue>? onlyAfter;
        bool shared;
        try
        {
            (onlyBefore, onlyAfter, shared) = Analyzer.CompareSatisfaction(before.Root, after.Root);
        }
        catch (BddNodeBudgetExceededException exception)
        {
            return new RuleRelationResult(RuleRelation.Undecided, null, null, exception.Message);
        }

        // The subset checks come first, so a rule that is never True is tighter (or equivalent), not disjoint.
        RuleRelation relation = (onlyBefore, onlyAfter) switch
        {
            (null, null) => RuleRelation.Equivalent,
            (_, null) => RuleRelation.Tighter,
            (null, _) => RuleRelation.Looser,
            _ => shared ? RuleRelation.Overlapping : RuleRelation.Disjoint,
        };
        return new RuleRelationResult(relation, Printed(onlyBefore), Printed(onlyAfter), null);
    }

    /// <summary>Gets why the rules are not analysed, or <see langword="null"/> when their distinct terms fit the cap.</summary>
    private static string? CapReason<TContext>(
        CompiledRule<TContext> first,
        CompiledRule<TContext> second,
        int? maxAnalysisTerms
    )
    {
        int cap = maxAnalysisTerms ?? CompilerOptions.Default.MaxAnalysisTerms;
        HashSet<TermIdentity> terms = Analyzer.DistinctTerms(first.Root);
        terms.UnionWith(Analyzer.DistinctTerms(second.Root));
        return terms.Count > cap
            ? $"The rules have {terms.Count} distinct terms between them, which exceeds the configured cap of {cap}."
            : null;
    }

    /// <summary>Keys an assignment by the printed term (such as <c>hasRole(role: "Y")</c>), so callers need no internal identity type.</summary>
    private static Dictionary<string, TruthValue>? Printed(IReadOnlyDictionary<TermIdentity, TruthValue>? assignment)
    {
        return assignment?.ToDictionary(p => p.Key.ToString(), p => p.Value);
    }
}
