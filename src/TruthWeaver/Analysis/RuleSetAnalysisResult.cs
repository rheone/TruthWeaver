namespace TruthWeaver.Analysis;

/// <summary>The result of <see cref="RuleSetAnalysis.Analyze{TContext}(IReadOnlyList{TruthWeaver.Evaluation.CompiledRule{TContext}}, int?, int?)"/>.</summary>
/// <param name="Outcome">The verdict. Check it before reading an empty list as "nothing found".</param>
/// <param name="Pairs">
/// The joint-outcome table of every unordered pair of rules, ordered by <see cref="RuleSetPair.First"/> and then by
/// <see cref="RuleSetPair.Second"/>: n(n-1)/2 entries for n rules. Present for <see cref="RuleSetAnalysisOutcome.Decided"/>
/// and <see cref="RuleSetAnalysisOutcome.Undecided"/>; empty for <see cref="RuleSetAnalysisOutcome.TooManyRules"/>.
/// </param>
/// <param name="Rules">
/// The redundancy of each rule, in index order, under the "any grants" reading. Empty unless <paramref name="Outcome"/>
/// is <see cref="RuleSetAnalysisOutcome.Decided"/>.
/// </param>
/// <param name="RemovableTogether">
/// The indexes, in ascending order, of one group of rules that can be removed at the same time without changing which
/// assignments satisfy the set under the "any grants" reading. It is found by a greedy walk from the end of the list
/// that removes a rule when the rules still kept cover it, so it is not always the largest such group. Empty unless
/// <paramref name="Outcome"/> is <see cref="RuleSetAnalysisOutcome.Decided"/>.
/// </param>
/// <param name="Shadowing">
/// Whether each rule is shadowed, in index order, when the list is read in order as "the first rule that applies
/// wins" and an earlier <c>True</c> or <c>Unknown</c> rule blocks every later rule. Empty unless
/// <paramref name="Outcome"/> is <see cref="RuleSetAnalysisOutcome.Decided"/>.
/// </param>
/// <param name="Reason">
/// For <see cref="RuleSetAnalysisOutcome.Undecided"/> and <see cref="RuleSetAnalysisOutcome.TooManyRules"/>, why the
/// set-level answers are missing; otherwise <see langword="null"/>.
/// </param>
public sealed record RuleSetAnalysisResult(
    RuleSetAnalysisOutcome Outcome,
    IReadOnlyList<RuleSetPair> Pairs,
    IReadOnlyList<RuleRedundancy> Rules,
    IReadOnlyList<int> RemovableTogether,
    IReadOnlyList<RuleShadowing> Shadowing,
    string? Reason
)
{
    /// <summary>Finds the joint-outcome table of two rules.</summary>
    /// <param name="first">The index of one rule.</param>
    /// <param name="second">The index of the other rule. The order of the two indexes does not matter.</param>
    /// <returns>
    /// The entry, with <see cref="RuleSetPair.First"/> the smaller index, or <see langword="null"/> when no entry exists
    /// (the indexes are equal or out of range, or <see cref="Outcome"/> is <see cref="RuleSetAnalysisOutcome.TooManyRules"/>).
    /// </returns>
    public RuleSetPair? FindPair(int first, int second)
    {
        (int low, int high) = first <= second ? (first, second) : (second, first);
        foreach (RuleSetPair pair in this.Pairs)
        {
            if (pair.First == low && pair.Second == high)
            {
                return pair;
            }
        }

        return null;
    }
}
