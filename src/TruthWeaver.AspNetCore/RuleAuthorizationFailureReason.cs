namespace TruthWeaver.AspNetCore;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// The reason that a <see cref="RuleRequirement"/> failed: the requirement and the full <see cref="Decision"/> of its rule,
/// with the faults and the trace. It is one of the <see cref="AuthorizationFailure.FailureReasons"/> of a failed check.
/// </summary>
/// <remarks>
/// Use <see cref="RuleAuthorizationFailureExtensions.GetRuleFailures(AuthorizationResult)"/> to read these reasons from an
/// <see cref="AuthorizationResult"/>. The decision can hold sensitive data (term arguments and fault messages), so write it to a
/// log, not to a response, unless you have checked what it holds.
/// </remarks>
/// <param name="handler">The handler that evaluated the rule.</param>
/// <param name="requirement">The requirement that failed.</param>
/// <param name="decision">The decision of the rule.</param>
/// <exception cref="ArgumentNullException"><paramref name="requirement"/> or <paramref name="decision"/> is <see langword="null"/>.</exception>
public sealed class RuleAuthorizationFailureReason(
    IAuthorizationHandler handler,
    RuleRequirement requirement,
    Decision decision
) : AuthorizationFailureReason(handler, MessageOf(requirement, decision))
{
    /// <summary>Gets the requirement that failed.</summary>
    public RuleRequirement Requirement { get; } = requirement;

    /// <summary>Gets the decision of the rule, with its faults and trace.</summary>
    public Decision Decision { get; } = decision;

    private static string MessageOf(RuleRequirement requirement, Decision decision)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        ArgumentNullException.ThrowIfNull(decision);
        return $"The rule requirement '{requirement.Name}' was {decision.Result}, with {decision.Faults.Count} fault(s).";
    }
}
