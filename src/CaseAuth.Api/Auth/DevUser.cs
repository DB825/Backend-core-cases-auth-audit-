namespace CaseAuth.Api.Auth;

public static class Roles
{
    public const string Analyst = "Analyst";
    public const string Supervisor = "Supervisor";
}

public record DevUser(string Id, string Username, string DisplayName, string FirmId, string Role);

// Development-only identities. There is no password or token: the DevAuthenticationHandler
// trusts the X-Dev-User header outright, which is only acceptable because it is wired up for
// the Development environment exclusively (see Program.cs). Replace with real OIDC for any
// other environment.
public static class DevUserStore
{
    public const string FirmA = "FIRM-A";
    public const string FirmB = "FIRM-B";

    public static readonly IReadOnlyDictionary<string, DevUser> Users = new List<DevUser>
    {
        new("u-analyst1", "analyst1", "Analyst One", FirmA, Roles.Analyst),
        new("u-analyst2", "analyst2", "Analyst Two", FirmB, Roles.Analyst),
        // Seeded with FIRM-A only, per the project brief's literal three-user seed list.
        // FIRM-B has no supervisor in this fixture set, so decisions on FIRM-B cases cannot
        // be approved/rejected until a second supervisor is seeded - documented limitation.
        new("u-supervisor", "supervisor", "Supervisor", FirmA, Roles.Supervisor),
    }.ToDictionary(u => u.Username, StringComparer.OrdinalIgnoreCase);

    public static bool TryGet(string username, out DevUser user) =>
        Users.TryGetValue(username, out user!);
}
