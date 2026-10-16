namespace TruthWeaver.Samples.AspNetCore;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using TruthWeaver.Abstractions;
using TruthWeaver.AspNetCore;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

/// <summary>
/// The sample application: three rule policies, the services they use and the endpoints. <c>Program.cs</c> and the
/// tests share it, so the tests run the same application on a test server.
/// </summary>
public static class DocumentApp
{
    /// <summary>An endpoint policy: any authenticated user may read a document.</summary>
    public const string CanRead = "CanRead";

    /// <summary>A resource-based policy: the owner of the document, or an editor, may change it.</summary>
    public const string CanEdit = "CanEdit";

    /// <summary>An endpoint policy with a service call: an operator who is on call may send a page.</summary>
    public const string CanPage = "CanPage";

    /// <summary>The rule text of <see cref="CanRead"/>.</summary>
    public const string CanReadRule = "isAuthenticated";

    /// <summary>The rule text of <see cref="CanEdit"/>.</summary>
    public const string CanEditRule = "isOwner OR isInRole(role: \"editor\")";

    /// <summary>
    /// The rule text of <see cref="CanPage"/>. When the roster service is offline, <c>isOnCall</c> is a fault, so the rule
    /// is <c>Unknown</c> for an operator, and the policy denies (fail-closed).
    /// </summary>
    public const string CanPageRule = "isInRole(role: \"operator\") AND isOnCall";

    /// <summary>Registers authentication, the rule policies, the problem-details responses and the sample services.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    public static IServiceCollection AddDocumentApp(this IServiceCollection services)
    {
        services.AddSingleton<DocumentStore>();
        services.AddSingleton<OnCallRoster>();

        // The registry records only the type of a class-based predicate; the container creates it.
        services.AddTransient<IsOnCall>();

        // One registry for every rule over a Document. The rules compile once, at startup.
        RuleCompiler<AuthorizationContext<Document>> compiler = new(
            PredicateRegistry<AuthorizationContext<Document>>
                .CreateBuilder()
                .AddAuthorizationPredicates()
                .AddIsOwner(document => document.OwnerId)
                .Add<IsOnCall>()
                .Build()
        );
        CompiledRule<AuthorizationContext<Document>> canRead = compiler.Compile(CanReadRule).GetRuleOrThrow();
        CompiledRule<AuthorizationContext<Document>> canEdit = compiler.Compile(CanEditRule).GetRuleOrThrow();
        CompiledRule<AuthorizationContext<Document>> canPage = compiler.Compile(CanPageRule).GetRuleOrThrow();

        services
            .AddAuthentication(HeaderAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(HeaderAuthenticationHandler.SchemeName, null);
        services.AddAuthorization(options =>
        {
            options.AddTruthWeaverPolicy(CanRead, canRead);
            options.AddTruthWeaverPolicy(CanEdit, canEdit);
            options.AddTruthWeaverPolicy(CanPage, canPage);
        });
        services.AddTruthWeaverAuthorization().AddTruthWeaverAuthorizationProblemDetails();
        return services;
    }

    /// <summary>Adds the middleware and maps the endpoints.</summary>
    /// <param name="app">The application.</param>
    /// <returns><paramref name="app"/>, for chaining.</returns>
    public static WebApplication UseDocumentApp(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();

        // Endpoint policy: the authorization middleware evaluates the rule before the handler runs.
        app.MapGet(
                "/documents/{id}",
                (string id, DocumentStore store) => store.Find(id) is { } document ? Results.Ok(document) : Results.NotFound()
            )
            .RequireAuthorization(CanRead);

        // Resource-based check: the handler loads the document, then asks the authorization service about it.
        app.MapPut("/documents/{id}", RenameAsync).RequireAuthorization(CanRead);

        // Fail-closed: when the roster service is offline, the rule is Unknown and the response is 403.
        app.MapPost("/pages", () => Results.Ok("paged")).RequireAuthorization(CanPage);
        return app;
    }

    private static async Task<IResult> RenameAsync(
        string id,
        string title,
        HttpContext http,
        DocumentStore store,
        IAuthorizationService authorization
    )
    {
        if (store.Find(id) is not { } document)
        {
            return Results.NotFound();
        }

        AuthorizationResult result = await authorization.AuthorizeAsync(http.User, document, CanEdit);
        if (!result.Succeeded)
        {
            // The decision is on the failure. Show the result only; the faults and the trace stay in the log.
            IReadOnlyList<RuleAuthorizationFailureReason> failures = result.GetRuleFailures();
            Decision? decision = failures.Count > 0 ? failures[0].Decision : null;
            return Results.Problem(
                detail: $"The authorization rule '{CanEdit}' was {decision?.Result}.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?> { ["result"] = decision?.Result.ToString() }
            );
        }

        store.Rename(document, title);
        return Results.NoContent();
    }
}
