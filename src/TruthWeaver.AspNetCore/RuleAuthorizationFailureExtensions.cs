namespace TruthWeaver.AspNetCore;

using Microsoft.AspNetCore.Authorization;

/// <summary>Reads the <see cref="RuleAuthorizationFailureReason"/> values of a failed authorization check.</summary>
public static class RuleAuthorizationFailureExtensions
{
    /// <summary>Gets the rule failures of an authorization failure, in the order the handler recorded them.</summary>
    /// <param name="failure">The failure, such as <see cref="AuthorizationResult.Failure"/>, or <see langword="null"/> for none.</param>
    /// <returns>The rule failures; empty when <paramref name="failure"/> is <see langword="null"/> or holds none.</returns>
    public static IReadOnlyList<RuleAuthorizationFailureReason> GetRuleFailures(this AuthorizationFailure? failure)
    {
        return failure is null ? [] : [.. failure.FailureReasons.OfType<RuleAuthorizationFailureReason>()];
    }

    /// <summary>Gets the rule failures of an authorization result.</summary>
    /// <param name="result">The result of <c>IAuthorizationService.AuthorizeAsync</c>.</param>
    /// <returns>The rule failures; empty when the check succeeded or no rule failed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<RuleAuthorizationFailureReason> GetRuleFailures(this AuthorizationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Failure.GetRuleFailures();
    }
}
