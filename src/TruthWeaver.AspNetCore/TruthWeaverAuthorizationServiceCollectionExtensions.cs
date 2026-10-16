namespace TruthWeaver.AspNetCore;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// <see cref="IServiceCollection"/> extensions that wire TruthWeaver rules into ASP.NET Core authorization. Registration is
/// explicit, as for <c>AddTruthWeaver</c>: these methods register services and scan nothing.
/// </summary>
public static class TruthWeaverAuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the handler that evaluates every <see cref="RuleRequirement"/>, and <c>IHttpContextAccessor</c>, which the
    /// handler uses to find the request of a resource-based check. Call it once; a second call adds nothing.
    /// </summary>
    /// <remarks>
    /// This method does not add authorization itself. The host calls <c>AddAuthorization</c> (or
    /// <c>AddAuthorizationBuilder</c>) and adds the policies with <c>AddTruthWeaverPolicy</c> or <c>RequireRule</c>.
    /// </remarks>
    /// <param name="services">The service collection to add to.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddTruthWeaverAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddHttpContextAccessor();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, RuleAuthorizationHandler>());
        return services;
    }

    /// <summary>
    /// Replaces the authorization middleware result handler with one that writes a problem-details body when a rule
    /// requirement denies a request, and adds the problem-details service. The status code stays the default one: 401
    /// when the user is not authenticated, 403 when the user is.
    /// </summary>
    /// <remarks>
    /// The body has the requirement name in <c>requirement</c> and the rule result in <c>result</c>, and no faults or
    /// trace. To add more, read the <see cref="RuleAuthorizationFailureFeature"/> of the request in a
    /// <c>ProblemDetailsOptions.CustomizeProblemDetails</c> callback. A scheme that redirects or writes its own body (a
    /// cookie scheme) keeps its response.
    /// </remarks>
    /// <param name="services">The service collection to add to.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddTruthWeaverAuthorizationProblemDetails(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddProblemDetails();
        services.Replace(ServiceDescriptor.Singleton<IAuthorizationMiddlewareResultHandler, RuleAuthorizationResultHandler>());
        return services;
    }
}
