namespace TruthWeaver.Analysis;

/// <summary>
/// Whether one rule of a set is redundant under the "any grants" reading: the set is satisfied when any of its rules is
/// <c>True</c>. A rule is redundant when every assignment of the terms that makes it <c>True</c> also makes some other
/// rule of the set <c>True</c>, so removing it alone does not change which assignments satisfy the set.
/// </summary>
/// <param name="Index">The index of the rule in the input list.</param>
/// <param name="IsRedundant">
/// <see langword="true"/> when the other rules cover this rule. This is a proof for every real application context,
/// provided the predicate-author contract holds. It is a statement about this rule alone, given every other rule: two
/// redundant rules can cover each other, so they cannot always both go. Use
/// <see cref="RuleSetAnalysisResult.RemovableTogether"/> for rules that can be removed at the same time.
/// </param>
/// <param name="Example">
/// When <paramref name="IsRedundant"/> is <see langword="false"/>, an assignment where this rule is <c>True</c> and every
/// other rule is not <c>True</c>: what only this rule lets in. It maps every distinct term of the set, keyed by its
/// printed form, to its value; terms the example does not depend on are <see cref="TruthValue.False"/>. This is a
/// candidate only, because terms are opaque and independent. <see langword="null"/> when the rule is redundant.
/// </param>
public sealed record RuleRedundancy(int Index, bool IsRedundant, IReadOnlyDictionary<string, TruthValue>? Example);
