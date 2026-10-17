namespace TruthWeaver.AspNetCore;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

/// <summary>
/// The authorization middleware result handler that <c>AddTruthWeaverAuthorizationProblemDetails</c> registers. It keeps
/// the default behavior (a challenge for an unauthenticated user, a forbid for an authenticated one), stores the rule
/// failures in a <see cref="RuleAuthorizationFailureFeature"/>, and then writes a problem-details body when the
/// authentication handler left a 401 or 403 response without a body.
/// </summary>
/// <param name="problemDetails">Writes the problem-details body.</param>
internal sealed class RuleAuthorizationResultHandler(IProblemDetailsService problemDetails)
    : IAuthorizationMiddlewareResultHandler
{
    /// <summary>The problem-details extension that holds the rule result.</summary>
    internal const string ResultExtension = "result";

    /// <summary>The problem-details extension that holds the requirement name.</summary>
    internal const string RequirementExtension = "requirement";

    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

    /// <inheritdoc/>
    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult
    )
    {
        // The handler records each rule failure of the request in the feature. A forbid result also carries them, which
        // covers a handler that ran without the request (no HTTP context accessor value).
        IReadOnlyList<RuleAuthorizationFailureReason> failures =
            context.Features.Get<RuleAuthorizationFailureFeature>()?.Failures
            ?? authorizeResult.AuthorizationFailure.GetRuleFailures();
        if (failures.Count > 0)
        {
            context.Features.Set(new RuleAuthorizationFailureFeature(failures));
        }

        await this.defaultHandler.HandleAsync(next, context, policy, authorizeResult).ConfigureAwait(false);

        // A scheme that redirects (a cookie scheme) or that wrote its own body has already decided the response.
        if (
            failures.Count == 0
            || context.Response.HasStarted
            || context.Response.StatusCode is not (StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        )
        {
            return;
        }

        RuleAuthorizationFailureReason first = failures[0];
        ProblemDetailsContext problem = new()
        {
            HttpContext = context,
            ProblemDetails =
            {
                Status = context.Response.StatusCode,
                Detail = $"The authorization rule '{first.Requirement.Name}' was {first.Decision.Result}.",
                Extensions =
                {
                    [RequirementExtension] = first.Requirement.Name,
                    [ResultExtension] = first.Decision.Result.ToString(),
                },
            },
        };
        await problemDetails.TryWriteAsync(problem).ConfigureAwait(false);
    }
}
