namespace TruthWeaver.Policies;

/// <summary>The result of evaluating a <see cref="PolicySet{TContext}"/> for one context.</summary>
/// <param name="Outcome">The combined outcome.</param>
/// <param name="Decision">
/// The outcome as a <see cref="Abstractions.Decision"/>: <see cref="TruthValue.True"/> for
/// <see cref="PolicyOutcome.Permit"/>, <see cref="TruthValue.False"/> for <see cref="PolicyOutcome.Deny"/> and
/// <see cref="PolicyOutcome.NotApplicable"/>, and <see cref="TruthValue.Unknown"/> for
/// <see cref="PolicyOutcome.Indeterminate"/>. Its faults are those of every rule that was evaluated. When the caller set
/// <c>EvaluationOptions.IncludeTrace</c>, its <see cref="Abstractions.Decision.Trace"/> is one flat trace of every rule
/// that was evaluated, in evaluation order, and each entry's text starts with the rule it came from
/// (<c>set target: </c>, <c>policy '&lt;id&gt;' target: </c> or <c>policy '&lt;id&gt;' condition: </c>); otherwise it is
/// <see langword="null"/>. Its <see cref="Abstractions.Decision.TraceTree"/> is always <see langword="null"/>. Because
/// <see cref="Abstractions.Decision.IsSatisfied"/> is fail-closed, only a permit is satisfied.
/// </param>
/// <param name="Results">The answer of each policy that was evaluated, in evaluation order. A policy that was not reached is absent.</param>
public sealed record PolicyDecision(PolicyOutcome Outcome, Decision Decision, EquatableArray<PolicyResult> Results)
{
    /// <summary>Gets a value indicating whether the set permits the request: only <see cref="PolicyOutcome.Permit"/> does.</summary>
    public bool IsPermitted => this.Decision.IsSatisfied;
}
