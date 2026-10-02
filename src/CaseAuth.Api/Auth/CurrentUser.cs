using System.Security.Claims;

namespace CaseAuth.Api.Auth;

public interface ICurrentUser
{
    string UserId { get; }
    string Username { get; }
    string FirmId { get; }
    string Role { get; }
    bool IsSupervisor { get; }
}

// Every controller/service must read the caller's firm from here - never from a route,
// query string, or request body parameter - so a caller cannot widen their own access by
// passing someone else's firm id.
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public const string FirmClaimType = "firm";

    private ClaimsPrincipal Principal => accessor.HttpContext?.User
        ?? throw new InvalidOperationException("No HTTP context is available.");

    public string UserId => Principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Caller has no NameIdentifier claim.");

    public string Username => Principal.FindFirstValue(ClaimTypes.Name)
        ?? throw new InvalidOperationException("Caller has no Name claim.");

    public string FirmId => Principal.FindFirstValue(FirmClaimType)
        ?? throw new InvalidOperationException("Caller has no firm claim.");

    public string Role => Principal.FindFirstValue(ClaimTypes.Role)
        ?? throw new InvalidOperationException("Caller has no role claim.");

    public bool IsSupervisor => Role == Roles.Supervisor;
}
