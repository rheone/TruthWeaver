namespace TruthWeaver.AspNetCore.Tests;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// A test-only authentication scheme: the <c>X-User</c> header names the user and <c>X-Roles</c> lists roles, comma
/// separated. A request without <c>X-User</c> is anonymous. Its challenge is 401 and its forbid is 403, with no body.
/// </summary>
internal sealed class HeaderAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>The scheme name.</summary>
    public const string SchemeName = "Header";

    /// <inheritdoc/>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? user = this.Request.Headers["X-User"];
        if (string.IsNullOrEmpty(user))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string[] roles = this.Request.Headers["X-Roles"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
        ClaimsPrincipal principal = TestSupport.User(user, roles);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
