namespace TruthWeaver.Analysis;

/// <summary>
/// The joint-outcome table of two rules of a set: the same answer <see cref="RuleOverlap.Compare{TContext}"/> gives
/// for the rule at <paramref name="First"/> and the rule at <paramref name="Second"/>.
/// </summary>
/// <param name="First">The index of the first rule in the input list. It is always less than <paramref name="Second"/>.</param>
/// <param name="Second">The index of the second rule in the input list.</param>
/// <param name="Overlap">
/// The reachable pairs of values, each with an example that names only the terms of these two rules. A reachable pair
/// is a candidate; a pair that is not reachable is a proof (ADR-0009 decision 4). Its outcome is
/// <see cref="RuleOverlapOutcome.Undecided"/>, with a reason, when this pair alone is over the term cap or the node
/// budget.
/// </param>
public sealed record RuleSetPair(int First, int Second, RuleOverlapResult Overlap);
