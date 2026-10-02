using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CaseAuth.Api.Auth;

public static class DevAuthDefaults
{
    public const string Scheme = "DevScheme";
    public const string HeaderName = "X-Dev-User";
}

// Resolves the caller from a plain header against the seeded DevUserStore. This exists so the
// rest of the app (controllers, services) can be written against ClaimsPrincipal/ICurrentUser
// the same way it would be against a real OIDC handler. Program.cs refuses to register this
// handler outside the Development environment.
public class DevAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(DevAuthDefaults.HeaderName, out var headerValues))
        {
            return Task.FromResult(AuthenticateResult.Fail(
                $"Missing required development header '{DevAuthDefaults.HeaderName}'."));
        }

        var username = headerValues.ToString();
        if (!DevUserStore.TryGet(username, out var user))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Unknown development user '{username}'."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(CurrentUser.FirmClaimType, user.FirmId),
        };

        var identity = new ClaimsIdentity(claims, DevAuthDefaults.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, DevAuthDefaults.Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
