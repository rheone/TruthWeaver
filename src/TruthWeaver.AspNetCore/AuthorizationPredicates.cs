namespace TruthWeaver.AspNetCore;

using System.Security.Claims;
using TruthWeaver.Registry;

/// <summary>
/// Ready-made predicates over <see cref="AuthorizationContext{TResource}"/>: <c>hasClaim</c>, <c>isInRole</c>,
/// <c>isAuthenticated</c> and <c>isOwner</c>. Register them on the registry builder of the rules that a policy evaluates.
/// </summary>
public static class AuthorizationPredicates
{
    /// <summary>The registered name of the claim predicate.</summary>
    public const string HasClaim = "hasClaim";

    /// <summary>The registered name of the role predicate.</summary>
    public const string IsInRole = "isInRole";

    /// <summary>The registered name of the authentication predicate.</summary>
    public const string IsAuthenticated = "isAuthenticated";

    /// <summary>The registered name of the owner predicate.</summary>
    public const string IsOwner = "isOwner";

    private static readonly PredicateSchema HasClaimSchema = new(
        HasClaim,
        "Has claim",
        "Does the user have a claim of the given type, and with the given value when a value is supplied?",
        [
            new PredicateArgumentSchema("type", "The claim type. The comparison ignores case.", LiteralKind.String),
            new PredicateArgumentSchema(
                "value",
                "The claim value, compared ordinally. When it is omitted, any value of the type matches.",
                LiteralKind.String,
                Required: false
            ),
        ]
    );

    private static readonly PredicateSchema IsInRoleSchema = new(
        IsInRole,
        "Is in role",
        "Is the user in the given role?",
        [new PredicateArgumentSchema("role", "The role name, as ClaimsPrincipal.IsInRole compares it.", LiteralKind.String)]
    );

    private static readonly PredicateSchema IsAuthenticatedSchema = PredicateSchema.NoArguments(
        IsAuthenticated,
        "Is authenticated",
        "Does the user have at least one authenticated identity?"
    );

    /// <summary>
    /// Registers <c>hasClaim(type, value?)</c>, <c>isInRole(role)</c> and <c>isAuthenticated</c>. Each answer is
    /// <see cref="TruthValue.True"/> or <see cref="TruthValue.False"/>: the claims of the user are known, so these
    /// predicates never answer <see cref="TruthValue.Unknown"/>.
    /// </summary>
    /// <typeparam name="TResource">The resource type of the authorization context.</typeparam>
    /// <param name="builder">The registry builder to add to.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A predicate with one of these names is already registered.</exception>
    public static PredicateRegistryBuilder<AuthorizationContext<TResource>> AddAuthorizationPredicates<TResource>(
        this PredicateRegistryBuilder<AuthorizationContext<TResource>> builder
    )
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add(
            HasClaimSchema,
            (context, args, _) =>
            {
                string type = args.GetString("type");

                // ClaimsPrincipal.HasClaim(type, value) compares the type ignoring case and the value ordinally;
                // FindFirst(type) uses the same type comparison when no value is supplied.
                bool found = args.TryGetString("value", out string? value)
                    ? context.User.HasClaim(type, value)
                    : context.User.FindFirst(type) is not null;
                return ValueTask.FromResult(ToTruth(found));
            }
        );
        builder.Add(
            IsInRoleSchema,
            (context, args, _) => ValueTask.FromResult(ToTruth(context.User.IsInRole(args.GetString("role"))))
        );
        builder.Add(IsAuthenticatedSchema, (context, _, _) => ValueTask.FromResult(ToTruth(IsAuthenticatedUser(context.User))));
        return builder;
    }

    /// <summary>
    /// Registers <c>isOwner</c>: is the user the owner of the resource? The predicate compares the owner identifier that
    /// <paramref name="ownerSelector"/> reads from the resource with the value of the first claim of type
    /// <paramref name="claimType"/>, ordinally.
    /// </summary>
    /// <remarks>
    /// The answer is <see cref="TruthValue.Unknown"/> when the check has no resource, when the resource has no owner
    /// identifier, or when the user has no claim of <paramref name="claimType"/>: without both identifiers, ownership
    /// cannot be decided. A policy denies <see cref="TruthValue.Unknown"/> unless it allows it.
    /// </remarks>
    /// <typeparam name="TResource">The resource type of the authorization context.</typeparam>
    /// <param name="builder">The registry builder to add to.</param>
    /// <param name="ownerSelector">Reads the owner identifier of a resource, or returns <see langword="null"/> for none.</param>
    /// <param name="claimType">The claim type of the user identifier. The default is <see cref="ClaimTypes.NameIdentifier"/>.</param>
    /// <returns><paramref name="builder"/>, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/>, <paramref name="ownerSelector"/> or <paramref name="claimType"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A predicate named <c>isOwner</c> is already registered.</exception>
    public static PredicateRegistryBuilder<AuthorizationContext<TResource>> AddIsOwner<TResource>(
        this PredicateRegistryBuilder<AuthorizationContext<TResource>> builder,
        Func<TResource, string?> ownerSelector,
        string claimType = ClaimTypes.NameIdentifier
    )
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(ownerSelector);
        ArgumentNullException.ThrowIfNull(claimType);

        PredicateSchema schema = PredicateSchema.NoArguments(
            IsOwner,
            "Is owner",
            $"Is the user (the claim '{claimType}') the owner of the resource?"
        );
        builder.Add(
            schema,
            (context, _, _) =>
            {
                string? owner = context.Resource is { } resource ? ownerSelector(resource) : null;
                string? user = context.User.FindFirst(claimType)?.Value;
                TruthValue result =
                    owner is null || user is null
                        ? TruthValue.Unknown
                        : ToTruth(string.Equals(owner, user, StringComparison.Ordinal));
                return ValueTask.FromResult(result);
            }
        );
        return builder;
    }

    /// <summary>The check of <c>DenyAnonymousAuthorizationRequirement</c>: any authenticated identity counts.</summary>
    private static bool IsAuthenticatedUser(ClaimsPrincipal user)
    {
        return user.Identities.Any(identity => identity.IsAuthenticated);
    }

    private static TruthValue ToTruth(bool value)
    {
        return value ? TruthValue.True : TruthValue.False;
    }
}
