namespace ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

/// <summary>
/// Institution-wide counts for the admin dashboard. The dictionaries are keyed by the enum name
/// of whatever they count, so a new status appears without a frontend change.
/// </summary>
public record DashboardSummaryDto(
    int TotalContainers,
    int ContainersInProgress,
    int ContainersCompleted,
    IReadOnlyDictionary<string, int> ContainersByPipelineStage,
    IReadOnlyDictionary<string, int> PublicationsByStatus,
    int PublishedPublicationsCount,
    IReadOnlyDictionary<string, int> EthicsApprovalsByStatus,
    int PendingCommitteeReviews,
    int CompletedCommitteeReviews,
    IReadOnlyDictionary<string, int> ReviewDecisionCounts);

/// <summary>
/// One entry in the institution-wide audit trail. Distinct from a publication's activity
/// history: this records every action against every entity, not just one publication's story.
/// </summary>
public record AuditLogEntryDto(
    Guid Id,
    string ActorName,
    string? OnBehalfOfName,
    string ActionType,
    string EntityType,
    Guid? EntityId,
    string? PreviousValue,
    string? NewValue,
    string? Comments,
    DateTime Timestamp);

/// <summary>
/// How many committee members of a given type a publication needs. RoleType matches the
/// committee member's type (internal or external).
/// </summary>
public record CommitteeRoleConfigDto(Guid? CommitteeId, string RoleType, int RequiredCount);

public record SetCommitteeRoleConfigRequestDto(string RoleType, int RequiredCount);

// ---------- User management ----------

/// <summary>
/// Granting a role to an existing account. Carries what the new role needs: a role without its
/// profile is one the person cannot use: a Coordinator with no profile is invisible to auto-
/// assignment, and a committee member with none cannot be put on a committee.
/// </summary>
public record ChangeUserRoleRequestDto(
    string Role,
    string Comments,
    Guid? DepartmentId = null,
    string? Affiliation = null);

public record UpdateUserRequestDto(string FirstName, string LastName, string? InstitutionalId, string Comments);

/// <summary>
/// An account created by an administrator rather than by sign-up. The backend marks it as
/// already verified, so this is the route for staff who will not register themselves.
/// Everything after Role is only meaningful for particular roles.
/// </summary>
public class CreateUserRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? InstitutionalId { get; set; }
    public string Role { get; set; } = string.Empty;

    public Guid? DepartmentId { get; set; }

    // Student
    public string? StudentIdNumber { get; set; }
    public string? Programme { get; set; }
    public string? Cohort { get; set; }

    // Supervisor
    public string? AreasOfExpertise { get; set; }
    public string? ResearchInterests { get; set; }

    // Committee member
    public string? CommitteeMemberType { get; set; }
    public string? Affiliation { get; set; }
}
