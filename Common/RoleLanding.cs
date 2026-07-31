using System.Security.Claims;

namespace ResearchPublicationManagementSystem.Common;

/// <summary>
/// Where each role lands after signing in. Kept in one place so the login redirect, the
/// sidebar/navbar brand link and the home route can't drift apart.
/// </summary>
public static class RoleLanding
{
    public static (string Controller, string Action) For(IEnumerable<string> roles)
    {
        var set = roles as IReadOnlyCollection<string> ?? roles.ToList();

        if (set.Contains(RoleNames.Student)) return ("Student", "student_dashboard");
        if (set.Contains(RoleNames.Coordinator)) return ("Coordinator", "Coordinator_dashboard");
        if (set.Contains(RoleNames.Supervisor)) return ("Supervisor", "SupervisorDashboard");
        if (set.Contains(RoleNames.HeadOfDepartment)) return ("HeadOfDepartment", "Head_of_Department_dashboard");
        // Both committee roles land on the same screens: they do the same job.
        if (set.Contains(RoleNames.ExternalCommitteeMember) || set.Contains(RoleNames.InternalCommitteeMember))
            return ("ExternalSupervisor", "External_Supervisor_Dashboard");
        if (set.Contains(RoleNames.Admin)) return ("Admin", "Dashboard");

        // Staff awaiting an operational role, or anything unmapped: their own profile is the
        // only thing they can meaningfully do.
        return ("Profile", "Me");
    }

    /// <summary>Where a visitor with no account belongs: the public catalogue.</summary>
    public static readonly (string Controller, string Action) Anonymous = ("Public", "public_catalogue");

    public static (string Controller, string Action) For(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true
            ? For(user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList())
            // Signed out, so there is no role to land on — and sending them to a page that
            // demands a login would make the brand link a trap.
            : Anonymous;
}
