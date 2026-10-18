namespace TruthWeaver.Analysis;

/// <summary>
/// Whether one rule of an ordered set is shadowed when the set is read as "the first rule that applies wins". An
/// earlier rule that is <c>True</c> or <c>Unknown</c> blocks every later rule, because a first-applicable walk never
/// skips an <c>Unknown</c> rule. A rule is shadowed when every assignment of the terms that makes it <c>True</c> also
/// makes some earlier rule <c>True</c> or <c>Unknown</c>, so the walk never reaches it with its own <c>True</c>.
/// </summary>
/// <param name="Index">The index of the rule in the input list.</param>
/// <param name="IsShadowed">
/// <see langword="true"/> when the earlier rules always block this rule. This is a proof for every real application
/// context, provided the predicate-author contract holds. A rule that is never <c>True</c> is shadowed, even at index
/// 0, because it never decides with its own <c>True</c>.
/// </param>
/// <param name="Example">
/// When <paramref name="IsShadowed"/> is <see langword="false"/>, an assignment where this rule is <c>True</c> and
/// every earlier rule is <c>False</c>: where the walk reaches this rule and it decides. It maps every distinct term of
/// the set, keyed by its printed form, to its value; terms the example does not depend on are
/// <see cref="TruthValue.False"/>. This is a candidate only, because terms are opaque and independent.
/// <see langword="null"/> when the rule is shadowed.
/// </param>
public sealed record RuleShadowing(int Index, bool IsShadowed, IReadOnlyDictionary<string, TruthValue>? Example);
