using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api;

namespace ResearchPublicationManagementSystem.Services;

/// <summary>Whether the person signed in could be put on an evaluation committee.</summary>
public interface ICommitteeEligibility
{
    Task<bool> IsCandidateAsync(ClaimsPrincipal user, CancellationToken ct = default);
}

/// <summary>
/// Asks the API whether this person is a committee candidate, and remembers the answer briefly.
///
/// The sidebar used to decide by role alone, which stopped being the whole rule once an
/// administrator could choose which roles committees are drawn from and leave individuals out. A
/// menu entry that ignores both leads somebody to a screen that will never have anything on it.
///
/// Read on every page, so it is cached. One minute, matching how the institution's details are
/// handled: an administrator narrowing the rule should see the entry go within a minute rather
/// than after a restart, and the alternative is an API call per page load per person.
/// </summary>
public class CommitteeEligibility(CommitteesApiClient committeesApi, IMemoryCache cache) : ICommitteeEligibility
{
    public async Task<bool> IsCandidateAsync(ClaimsPrincipal user, CancellationToken ct = default)
    {
        // Answered here rather than by asking, because these are the common cases and the API would
        // only say the same thing. Somebody holding no operational role is never a candidate: a
        // student is the subject of the work, and an account still on the placeholder role has not
        // been given a job yet.
        if (user.Identity?.IsAuthenticated != true) return false;
        if (!RoleNames.Operational.Any(user.IsInRole)) return false;

        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(id)) return false;

        var key = $"committee-candidate:{id}";
        if (cache.TryGetValue(key, out bool cached)) return cached;

        var result = await committeesApi.GetMyEligibilityAsync(ct);

        // A failure reads as "not a candidate": the entry simply does not appear for a minute,
        // which is a quieter wrong answer than a menu item leading to an error page.
        var isCandidate = result.Success && result.Data;

        cache.Set(key, isCandidate, TimeSpan.FromMinutes(1));
        return isCandidate;
    }
}
