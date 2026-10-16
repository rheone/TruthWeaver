namespace TruthWeaver.Samples.AspNetCore.Tests;

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The sample application on a test server: an endpoint policy, a resource-based check and the fail-closed behavior when a
/// predicate faults.
/// </summary>
public sealed class DocumentAppTests
{
    /// <summary>An anonymous request to a <c>CanRead</c> endpoint gets 401: the rule is <c>False</c> and the user is not authenticated.</summary>
    [Fact]
    public async Task GetDocument_AnonymousUser_Returns401_Test()
    {
        await using WebApplication app = await StartAsync();

        using HttpResponseMessage response = await SendAsync(app, HttpMethod.Get, "/documents/doc-1", user: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>An authenticated user meets <c>CanRead</c> and reads the document.</summary>
    [Fact]
    public async Task GetDocument_AuthenticatedUser_Returns200_Test()
    {
        await using WebApplication app = await StartAsync();

        using HttpResponseMessage response = await SendAsync(app, HttpMethod.Get, "/documents/doc-1", "bob");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>The owner of the document meets the resource-based <c>CanEdit</c> rule through <c>isOwner</c>.</summary>
    [Fact]
    public async Task RenameDocument_Owner_Returns204_Test()
    {
        await using WebApplication app = await StartAsync();

        using HttpResponseMessage response = await SendAsync(app, HttpMethod.Put, "/documents/doc-1?title=New", "alice");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>An editor meets <c>CanEdit</c> for a document that another user owns.</summary>
    [Fact]
    public async Task RenameDocument_Editor_Returns204_Test()
    {
        await using WebApplication app = await StartAsync();

        using HttpResponseMessage response = await SendAsync(
            app,
            HttpMethod.Put,
            "/documents/doc-1?title=New",
            "dave",
            "editor"
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>Another user fails <c>CanEdit</c>: the endpoint returns 403 with the <c>False</c> result from the decision.</summary>
    [Fact]
    public async Task RenameDocument_OtherUser_Returns403WithResult_Test()
    {
        await using WebApplication app = await StartAsync();

        using HttpResponseMessage response = await SendAsync(app, HttpMethod.Put, "/documents/doc-1?title=New", "bob");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("False", await ReadResultAsync(response));
    }

    /// <summary>An operator who is on call meets <c>CanPage</c>.</summary>
    [Fact]
    public async Task Page_OperatorOnCall_Returns200_Test()
    {
        await using WebApplication app = await StartAsync();

        using HttpResponseMessage response = await SendAsync(app, HttpMethod.Post, "/pages", "carol", "operator");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// When the roster service is offline, <c>isOnCall</c> faults, the rule is <c>Unknown</c>, and the policy denies with
    /// 403 and a problem-details body: fail-closed.
    /// </summary>
    [Fact]
    public async Task Page_RosterOffline_Returns403Unknown_Test()
    {
        await using WebApplication app = await StartAsync();
        app.Services.GetRequiredService<OnCallRoster>().Offline = true;

        using HttpResponseMessage response = await SendAsync(app, HttpMethod.Post, "/pages", "carol", "operator");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("Unknown", await ReadResultAsync(response));
    }

    private static async Task<WebApplication> StartAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddDocumentApp();
        WebApplication app = builder.Build();
        app.UseDocumentApp();
        await app.StartAsync(TestContext.Current.CancellationToken);
        return app;
    }

    private static async Task<HttpResponseMessage> SendAsync(
        WebApplication app,
        HttpMethod method,
        string path,
        string? user,
        string? roles = null
    )
    {
        using HttpClient client = app.GetTestClient();
        using HttpRequestMessage request = new(method, path);
        if (user is not null)
        {
            request.Headers.Add("X-User", user);
        }

        if (roles is not null)
        {
            request.Headers.Add("X-Roles", roles);
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<string?> ReadResultAsync(HttpResponseMessage response)
    {
        using JsonDocument body = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
        );
        return body.RootElement.GetProperty("result").GetString();
    }
}
