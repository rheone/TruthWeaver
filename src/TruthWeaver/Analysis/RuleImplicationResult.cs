namespace TruthWeaver.Analysis;

/// <summary>The result of <see cref="RuleImplication.Implies{TContext}"/>.</summary>
/// <param name="Outcome">The verdict.</param>
/// <param name="CounterExample">
/// For <see cref="RuleImplicationOutcome.DoesNotHold"/>, an assignment that makes the first rule <c>True</c> and the second
/// rule <c>False</c> or <c>Unknown</c>: every distinct term in either rule (keyed by its printed form, such as
/// <c>hasRole(role: "Y")</c>) mapped to the value it takes. Terms the counter-example does not depend on are
/// <see cref="TruthValue.False"/>. <see langword="null"/> otherwise.
/// </param>
/// <param name="Reason">For <see cref="RuleImplicationOutcome.Undecided"/>, why no verdict was reached; otherwise <see langword="null"/>.</param>
public sealed record RuleImplicationResult(
    RuleImplicationOutcome Outcome,
    IReadOnlyDictionary<string, TruthValue>? CounterExample,
    string? Reason
);
