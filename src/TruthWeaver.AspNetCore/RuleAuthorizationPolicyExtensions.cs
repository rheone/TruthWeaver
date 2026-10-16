namespace TruthWeaver.AspNetCore;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using TruthWeaver.Evaluation;

/// <summary>
/// Adds rule requirements to authorization policies. A policy built here works with <c>[Authorize(Policy = ...)]</c>,
/// <c>RequireAuthorization</c>, <c>IAuthorizationService.AuthorizeAsync(user, resource, policy)</c> and
/// <c>AuthorizeView</c>, because the rule is an ordinary <see cref="IAuthorizationRequirement"/>.
/// </summary>
public static class RuleAuthorizationPolicyExtensions
{
    /// <summary>
    /// Adds a requirement that <paramref name="rule"/> decides over an <see cref="AuthorizationContext{TResource}"/>. The
    /// context holds the user, the resource of the check when it is a <typeparamref name="TResource"/>, and the request.
    /// </summary>
    /// <typeparam name="TResource">The resource type of the authorization context.</typeparam>
    /// <param name="builder">The policy builder.</param>
    /// <param name="rule">The compiled rule.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="RuleRequirementOptions.Default"/>.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="rule"/> is <see langword="null"/>.</exception>
    public static AuthorizationPolicyBuilder RequireRule<TResource>(
        this AuthorizationPolicyBuilder builder,
        CompiledRule<AuthorizationContext<TResource>> rule,
        RuleRequirementOptions? options = null
    )
    {
        return builder.RequireRule(rule, MapAuthorizationContext<TResource>, options);
    }

    /// <summary>
    /// Adds a requirement that <paramref name="rule"/> decides over a context of the host's own type, which
    /// <paramref name="map"/> builds from the authorization check and the HTTP context.
    /// </summary>
    /// <typeparam name="TContext">The context type of the rule.</typeparam>
    /// <param name="builder">The policy builder.</param>
    /// <param name="rule">The compiled rule.</param>
    /// <param name="map">
    /// Builds the context. It gets the check (with the user and the resource) and the HTTP context, or
    /// <see langword="null"/> outside a request. An exception it throws is not a fault: it fails the check.
    /// </param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="RuleRequirementOptions.Default"/>.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/>, <paramref name="rule"/> or <paramref name="map"/> is <see langword="null"/>.</exception>
    public static AuthorizationPolicyBuilder RequireRule<TContext>(
        this AuthorizationPolicyBuilder builder,
        CompiledRule<TContext> rule,
        Func<AuthorizationHandlerContext, HttpContext?, TContext> map,
        RuleRequirementOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddRequirements(new RuleRequirement<TContext>(rule, map, options));
    }

    /// <summary>
    /// Adds a policy named <paramref name="name"/> with one requirement that <paramref name="rule"/> decides over an
    /// <see cref="AuthorizationContext{TResource}"/>. The requirement takes the policy name unless
    /// <see cref="RuleRequirementOptions.Name"/> is set.
    /// </summary>
    /// <typeparam name="TResource">The resource type of the authorization context.</typeparam>
    /// <param name="authorization">The authorization options.</param>
    /// <param name="name">The policy name.</param>
    /// <param name="rule">The compiled rule.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="RuleRequirementOptions.Default"/>.</param>
    /// <returns><paramref name="authorization"/>, for chaining.</returns>
    /// <exception cref="ArgumentNullException">An argument other than <paramref name="options"/> is <see langword="null"/>.</exception>
    public static AuthorizationOptions AddTruthWeaverPolicy<TResource>(
        this AuthorizationOptions authorization,
        string name,
        CompiledRule<AuthorizationContext<TResource>> rule,
        RuleRequirementOptions? options = null
    )
    {
        return authorization.AddTruthWeaverPolicy(name, rule, MapAuthorizationContext<TResource>, options);
    }

    /// <summary>
    /// Adds a policy named <paramref name="name"/> with one requirement that <paramref name="rule"/> decides over a context
    /// of the host's own type, which <paramref name="map"/> builds. The requirement takes the policy name unless
    /// <see cref="RuleRequirementOptions.Name"/> is set.
    /// </summary>
    /// <typeparam name="TContext">The context type of the rule.</typeparam>
    /// <param name="authorization">The authorization options.</param>
    /// <param name="name">The policy name.</param>
    /// <param name="rule">The compiled rule.</param>
    /// <param name="map">Builds the context, as for <see cref="RequireRule{TContext}(AuthorizationPolicyBuilder, CompiledRule{TContext}, Func{AuthorizationHandlerContext, HttpContext, TContext}, RuleRequirementOptions)"/>.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="RuleRequirementOptions.Default"/>.</param>
    /// <returns><paramref name="authorization"/>, for chaining.</returns>
    /// <exception cref="ArgumentNullException">An argument other than <paramref name="options"/> is <see langword="null"/>.</exception>
    public static AuthorizationOptions AddTruthWeaverPolicy<TContext>(
        this AuthorizationOptions authorization,
        string name,
        CompiledRule<TContext> rule,
        Func<AuthorizationHandlerContext, HttpContext?, TContext> map,
        RuleRequirementOptions? options = null
    )
    {
        ArgumentNullException.ThrowIfNull(authorization);
        ArgumentNullException.ThrowIfNull(name);
        RuleRequirementOptions named = options ?? RuleRequirementOptions.Default;
        if (named.Name is null)
        {
            named = named with { Name = name };
        }

        authorization.AddPolicy(name, policy => policy.RequireRule(rule, map, named));
        return authorization;
    }

    /// <summary>
    /// Builds an <see cref="AuthorizationContext{TResource}"/>. The resource is the check's resource when it has type
    /// <typeparamref name="TResource"/>; an endpoint policy passes the HTTP context instead, which gives no resource.
    /// </summary>
    private static AuthorizationContext<TResource> MapAuthorizationContext<TResource>(
        AuthorizationHandlerContext context,
        HttpContext? http
    )
    {
        TResource? resource = context.Resource is TResource typed ? typed : default;
        return new AuthorizationContext<TResource>(context.User, resource, http);
    }
}
