namespace TruthWeaver.AspNetCore;

/// <summary>
/// An HTTP feature that holds the rule failures of a request. The rule handler adds each failure of a check that runs in
/// a request, for an endpoint policy and for a resource-based check. A <c>ProblemDetailsOptions.CustomizeProblemDetails</c>
/// callback, an <c>IAuthorizationMiddlewareResultHandler</c> or later middleware reads the decisions with
/// <c>HttpContext.Features.Get&lt;RuleAuthorizationFailureFeature&gt;()</c>. It is the only place for the decision of a
/// challenge (401), because a challenge result has no <c>AuthorizationFailure</c>.
/// </summary>
/// <param name="Failures">The rule failures of the request, in the order of the checks; never empty.</param>
public sealed record RuleAuthorizationFailureFeature(IReadOnlyList<RuleAuthorizationFailureReason> Failures);
