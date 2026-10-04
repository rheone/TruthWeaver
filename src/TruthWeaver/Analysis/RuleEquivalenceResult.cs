namespace TruthWeaver.Analysis;

using TruthWeaver.Abstractions;

/// <summary>The result of <see cref="RuleEquivalence.Compare{TContext}"/>.</summary>
/// <param name="Outcome">The verdict.</param>
/// <param name="CounterExample">
/// For <see cref="RuleEquivalenceOutcome.NotEquivalent"/>, an assignment that makes the two rules differ: every distinct
/// term in either rule (keyed by its printed form, e.g. <c>hasRole(role: "Y")</c>) mapped to the value it takes. Terms the
/// difference does not depend on are <see cref="TruthValue.False"/>. <see langword="null"/> otherwise.
/// </param>
/// <param name="Reason">For <see cref="RuleEquivalenceOutcome.Undecided"/>, why no verdict was reached; otherwise <see langword="null"/>.</param>
public sealed record RuleEquivalenceResult(
    RuleEquivalenceOutcome Outcome,
    IReadOnlyDictionary<string, TruthValue>? CounterExample,
    string? Reason
);
