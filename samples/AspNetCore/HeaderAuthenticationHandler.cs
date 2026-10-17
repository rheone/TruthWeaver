namespace TruthWeaver.Samples.AspNetCore;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

/// <summary>
/// A demonstration authentication scheme. Do not use it in production: it trusts the request. The <c>X-User</c> header
/// names the user, and <c>X-Roles</c> lists roles, comma separated. A request without <c>X-User</c> is anonymous. The
/// challenge is 401 and the forbid is 403, with no body.
/// </summary>
/// <param name="options">The scheme options.</param>
/// <param name="logger">The logger factory.</param>
/// <param name="encoder">The URL encoder.</param>
public sealed class HeaderAuthenticationHandler(
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

        List<Claim> claims = [new(ClaimTypes.NameIdentifier, user), new(ClaimTypes.Name, user)];
        claims.AddRange(
            this.Request.Headers["X-Roles"]
                .ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(role => new Claim(ClaimTypes.Role, role))
        );
        ClaimsPrincipal principal = new(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
