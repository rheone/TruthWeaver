namespace TruthWeaver.AspNetCore.Tests;

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TruthWeaver.Abstractions;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

/// <summary>
/// Rule policies on endpoints, through a real request pipeline on <see cref="TestServer"/>: the status code is the default
/// one (401 or 403), and the problem-details body and the <see cref="RuleAuthorizationFailureFeature"/> carry the decision.
/// </summary>
public sealed class RuleAuthorizationProblemDetailsTests
{
    private const string Policy = "editors";

    /// <summary>An authenticated user whose rule is <c>False</c> gets 403 with a problem-details body that names the result.</summary>
    [Fact]
    public async Task Request_AuthenticatedUserDenied_Returns403ProblemDetails_Test()
    {
        await using WebApplication app = await StartAsync("isInRole(role: \"editor\")");
        using HttpClient client = app.GetTestClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "/secret");
        request.Headers.Add("X-User", "alice");

        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using JsonDocument body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
        );
        Assert.Equal(403, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("False", body.RootElement.GetProperty("result").GetString());
        Assert.Equal(Policy, body.RootElement.GetProperty("requirement").GetString());
    }

    /// <summary>An anonymous user whose rule fails gets 401 (a challenge), with a problem-details body.</summary>
    [Fact]
    public async Task Request_AnonymousUserDenied_Returns401_Test()
    {
        await using WebApplication app = await StartAsync("isAuthenticated");
        using HttpClient client = app.GetTestClient();

        using HttpResponseMessage response = await client.GetAsync(
            new Uri("/secret", UriKind.Relative),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using JsonDocument body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
        );
        Assert.Equal("False", body.RootElement.GetProperty("result").GetString());
    }

    /// <summary>A user whose rule is <c>True</c> reaches the endpoint.</summary>
    [Fact]
    public async Task Request_RuleTrue_ReachesEndpoint_Test()
    {
        await using WebApplication app = await StartAsync("isInRole(role: \"editor\")");
        using HttpClient client = app.GetTestClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "/secret");
        request.Headers.Add("X-User", "alice");
        request.Headers.Add("X-Roles", "editor");

        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("secret", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// A <c>CustomizeProblemDetails</c> callback reads the decision from <see cref="RuleAuthorizationFailureFeature"/>, so a
    /// host can add the fault count to the body.
    /// </summary>
    [Fact]
    public async Task Request_DeniedWithFault_FeatureCarriesDecisionToCustomizer_Test()
    {
        await using WebApplication app = await StartAsync(
            "isOnCall",
            configureRegistry: true,
            customize: context =>
            {
                if (context.HttpContext.Features.Get<RuleAuthorizationFailureFeature>() is { } feature)
                {
                    context.ProblemDetails.Extensions["faults"] = feature.Failures[0].Decision.Faults.Count;
                }
            }
        );
        using HttpClient client = app.GetTestClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "/secret");
        request.Headers.Add("X-User", "alice");

        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using JsonDocument body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
        );
        Assert.Equal("Unknown", body.RootElement.GetProperty("result").GetString());
        Assert.Equal(1, body.RootElement.GetProperty("faults").GetInt32());
    }

    /// <summary>An endpoint policy gives the rule the request: <see cref="AuthorizationContext{TResource}.Http"/> is set.</summary>
    [Fact]
    public async Task Request_EndpointPolicy_RuleSeesHttpContext_Test()
    {
        await using WebApplication app = await StartAsync("hasRequest", configureRegistry: true);
        using HttpClient client = app.GetTestClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "/secret");
        request.Headers.Add("X-User", "alice");

        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<WebApplication> StartAsync(
        string ruleText,
        bool configureRegistry = false,
        Action<ProblemDetailsContext>? customize = null
    )
    {
        // The extra predicates: one that faults, and one that reports whether the rule sees the request.
        static void AddExtraPredicates(PredicateRegistryBuilder<AuthorizationContext<Document>> registry)
        {
            TestSupport.AddFaulting(registry, "isOnCall");
            registry.Add(
                PredicateSchema.NoArguments("hasRequest", "Has request", "Does the check run inside a request?"),
                (context, _, _) => ValueTask.FromResult(context.Http is null ? TruthValue.False : TruthValue.True)
            );
        }

        CompiledRule<AuthorizationContext<Document>> rule = TestSupport.Compile(
            ruleText,
            configureRegistry ? AddExtraPredicates : null
        );

        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder
            .Services.AddAuthentication(HeaderAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(HeaderAuthenticationHandler.SchemeName, null);
        builder.Services.AddAuthorization(options => options.AddTruthWeaverPolicy(Policy, rule));
        builder.Services.AddTruthWeaverAuthorization().AddTruthWeaverAuthorizationProblemDetails();
        if (customize is not null)
        {
            builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = customize);
        }

        WebApplication app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/secret", () => "secret").RequireAuthorization(Policy);
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }
}
