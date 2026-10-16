namespace TruthWeaver.Analysis;

/// <summary>
/// One reachable pair of a <see cref="RuleOverlapResult"/>'s joint-outcome table (ADR-0009 decision 1) and an
/// example assignment that produces it.
/// </summary>
/// <param name="First">The value the first rule takes under <paramref name="Example"/>.</param>
/// <param name="Second">The value the second rule takes under <paramref name="Example"/>.</param>
/// <param name="Example">
/// An assignment of the terms that produces this pair: every distinct term in either rule (keyed by its printed
/// form, such as <c>hasRole(role: "Y")</c>) mapped to the value it takes. Terms the pair does not depend on are
/// <see cref="TruthValue.False"/>. This is a <em>candidate</em> only, not a proof: terms are opaque and independent
/// (<c>CONTEXT.md</c>, Term identity), so the assignment can be impossible in the application (ADR-0009 decision 4).
/// </param>
public sealed record JointOutcome(TruthValue First, TruthValue Second, IReadOnlyDictionary<string, TruthValue> Example);
