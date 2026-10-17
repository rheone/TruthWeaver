namespace TruthWeaver.AspNetCore;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

/// <summary>
/// The evaluation context of an authorization rule: the user, the resource that the check is about, and the HTTP request
/// when one exists. The ready-made predicates of <see cref="AuthorizationPredicates"/> read it.
/// </summary>
/// <typeparam name="TResource">The type of the resource that the rule checks, such as a document or an order.</typeparam>
/// <param name="User">The user of the authorization check.</param>
/// <param name="Resource">
/// The resource of a resource-based check (<c>IAuthorizationService.AuthorizeAsync(user, resource, policy)</c>). It is
/// <see langword="null"/> (or the default value of a value type) when the check has no resource of type
/// <typeparamref name="TResource"/>, for example an endpoint policy, where the resource is the <see cref="HttpContext"/>.
/// </param>
/// <param name="Http">The HTTP context of the request, or <see langword="null"/> when the check runs outside a request.</param>
public sealed record AuthorizationContext<TResource>(ClaimsPrincipal User, TResource? Resource, HttpContext? Http);
