namespace TruthWeaver.AspNetCore;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Evaluates every pending <see cref="RuleRequirement"/> of an authorization check. A requirement whose rule meets it
/// succeeds. Any other result fails the check with a <see cref="RuleAuthorizationFailureReason"/> that keeps the decision,
/// so the check is fail-closed: <c>False</c>, <c>Unknown</c> (unless allowed) and faults that make the result
/// <c>Unknown</c> all deny.
/// </summary>
/// <param name="services">The root service provider, used only when the check runs outside a request.</param>
/// <param name="httpContextAccessor">Finds the request of a resource-based check, whose resource is not the HTTP context.</param>
/// <param name="logger">Receives one entry for each denied requirement.</param>
internal sealed class RuleAuthorizationHandler(
    IServiceProvider services,
    IHttpContextAccessor httpContextAccessor,
    ILogger<RuleAuthorizationHandler> logger
) : IAuthorizationHandler
{
    /// <inheritdoc/>
    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        // An endpoint policy passes the HTTP context as the resource; a resource-based check passes its own resource,
        // so the request comes from the accessor.
        HttpContext? http = context.Resource as HttpContext ?? httpContextAccessor.HttpContext;
        IServiceProvider evaluationServices = http?.RequestServices ?? services;
        CancellationToken cancellationToken = http?.RequestAborted ?? CancellationToken.None;

        // Succeed changes PendingRequirements, so take a copy before the loop.
        RuleRequirement[] requirements = [.. context.PendingRequirements.OfType<RuleRequirement>()];
        foreach (RuleRequirement requirement in requirements)
        {
            Decision decision = await requirement
                .EvaluateAsync(context, http, evaluationServices, cancellationToken)
                .ConfigureAwait(false);
            if (requirement.IsMetBy(decision))
            {
                context.Succeed(requirement);
            }
            else
            {
                RuleAuthorizationLog.RequirementDenied(logger, requirement.Name, decision.Result, decision.Faults.Count);
                RuleAuthorizationFailureReason reason = new(this, requirement, decision);
                context.Fail(reason);

                // A challenge result (401) carries no AuthorizationFailure, so the request feature is the only way for the
                // result handler and later middleware to see the decision of an unauthenticated user.
                if (http is not null)
                {
                    IReadOnlyList<RuleAuthorizationFailureReason> earlier =
                        http.Features.Get<RuleAuthorizationFailureFeature>()?.Failures ?? [];
                    http.Features.Set(new RuleAuthorizationFailureFeature([.. earlier, reason]));
                }
            }
        }
    }
}
