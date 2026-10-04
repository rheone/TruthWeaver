namespace TruthWeaver.Analysis;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;

/// <summary>
/// Decides whether two compiled rules are equivalent under Strong Kleene (K3) logic: they produce the same
/// <see cref="TruthValue"/> for every assignment of <c>True</c>, <c>False</c> or <c>Unknown</c> to their terms. It is
/// built on the same dual-rail BDD the analyzer uses, so it is exact (not sampled) and does not enumerate truth tables.
/// </summary>
/// <remarks>
/// Equivalence is over the terms as values: two terms are the same variable when they have the same
/// <see cref="TermIdentity"/> (predicate name and arguments), and different terms are treated as independent. The check
/// cannot know that two different predicates are related. It does not look at evaluation order, short-circuiting or
/// faults, only at the value each rule computes.
/// </remarks>
public static class RuleEquivalence
{
    /// <summary>Compares two compiled rules for Strong K3 equivalence.</summary>
    /// <typeparam name="TContext">The application context type both rules were compiled for.</typeparam>
    /// <param name="first">The first rule.</param>
    /// <param name="second">The second rule.</param>
    /// <param name="options">
    /// The bounds to apply; only <see cref="CompilerOptions.MaxAnalysisTerms"/> is used. Defaults to
    /// <see cref="CompilerOptions"/> defaults when <see langword="null"/>.
    /// </param>
    /// <returns>
    /// The verdict: <see cref="RuleEquivalenceOutcome.Equivalent"/>, <see cref="RuleEquivalenceOutcome.NotEquivalent"/> with
    /// a counter-example, or <see cref="RuleEquivalenceOutcome.Undecided"/> when the rules have more distinct terms
    /// (counted across both rules) than <see cref="CompilerOptions.MaxAnalysisTerms"/>. It never throws for a normal rule.
    /// </returns>
    /// <exception cref="ArgumentNullException">A rule is <see langword="null"/>.</exception>
    public static RuleEquivalenceResult Compare<TContext>(
        CompiledRule<TContext> first,
        CompiledRule<TContext> second,
        CompilerOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        int cap = (options ?? new CompilerOptions()).MaxAnalysisTerms;
        HashSet<TermIdentity> terms = Analyzer.DistinctTerms(first.Root);
        terms.UnionWith(Analyzer.DistinctTerms(second.Root));
        if (terms.Count > cap)
        {
            return new RuleEquivalenceResult(
                RuleEquivalenceOutcome.Undecided,
                null,
                $"The rules have {terms.Count} distinct terms between them, which exceeds the configured cap of {cap}."
            );
        }

        IReadOnlyDictionary<TermIdentity, TruthValue>? difference = Analyzer.FindDifference(first.Root, second.Root);
        if (difference is null)
        {
            return new RuleEquivalenceResult(RuleEquivalenceOutcome.Equivalent, null, null);
        }

        // Key by the printed term (e.g. hasRole(role: "Y")) so callers need no internal identity type.
        Dictionary<string, TruthValue> counterExample = difference.ToDictionary(p => p.Key.ToString(), p => p.Value);
        return new RuleEquivalenceResult(RuleEquivalenceOutcome.NotEquivalent, counterExample, null);
    }
}
