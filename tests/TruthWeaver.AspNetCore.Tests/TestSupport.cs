namespace TruthWeaver.AspNetCore.Tests;

using System.Security.Claims;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

/// <summary>Builds users and rules over <see cref="AuthorizationContext{TResource}"/> of <see cref="Document"/>.</summary>
internal static class TestSupport
{
    /// <summary>The authentication type of an authenticated test user.</summary>
    public const string AuthenticationType = "Test";

    /// <summary>Creates an authenticated user with an identifier claim, roles and more claims.</summary>
    public static ClaimsPrincipal User(string id, IEnumerable<string>? roles = null, params Claim[] claims)
    {
        List<Claim> all = [new(ClaimTypes.NameIdentifier, id), .. claims];
        all.AddRange((roles ?? []).Select(role => new Claim(ClaimTypes.Role, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(all, AuthenticationType));
    }

    /// <summary>Creates an anonymous user: one identity with no authentication type.</summary>
    public static ClaimsPrincipal Anonymous()
    {
        return new ClaimsPrincipal(new ClaimsIdentity());
    }

    /// <summary>
    /// Compiles rule text over the ready-made predicates, <c>isOwner</c> on <see cref="Document.OwnerId"/>, and any
    /// predicates that <paramref name="configure"/> adds.
    /// </summary>
    public static CompiledRule<AuthorizationContext<Document>> Compile(
        string text,
        Action<PredicateRegistryBuilder<AuthorizationContext<Document>>>? configure = null
    )
    {
        PredicateRegistryBuilder<AuthorizationContext<Document>> builder = PredicateRegistry<AuthorizationContext<Document>>
            .CreateBuilder()
            .AddAuthorizationPredicates()
            .AddIsOwner(document => document.OwnerId);
        configure?.Invoke(builder);
        return new RuleCompiler<AuthorizationContext<Document>>(builder.Build()).Compile(text).GetRuleOrThrow();
    }

    /// <summary>Adds a fake predicate that answers <paramref name="result"/>.</summary>
    public static void AddReturning<TContext>(PredicateRegistryBuilder<TContext> builder, string name, TruthValue result)
    {
        (PredicateSchema schema, Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            FakePredicates.Returning<TContext>(name, result);
        builder.Add(schema, evaluate);
    }

    /// <summary>Adds a fake predicate that throws, so its term is a fault.</summary>
    public static void AddFaulting<TContext>(PredicateRegistryBuilder<TContext> builder, string name)
    {
        (PredicateSchema schema, Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
            FakePredicates.Faulting<TContext>(name, new InvalidOperationException("The roster service is offline."));
        builder.Add(schema, evaluate);
    }
}
