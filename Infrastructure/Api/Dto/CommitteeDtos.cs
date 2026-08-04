namespace ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

/// <summary>
/// One member of an evaluation committee and where their decision stands. Decision is "Pending"
/// until they have voted, so a committee's own progress is readable from its members.
/// </summary>
public record CommitteeMemberDto(
    Guid UserId,
    string Name,
    string RoleType,
    string Decision,
    string? DecisionComments,
    DateTime? DecidedAt)
{
    public bool HasDecided => DecidedAt is not null;
}

public record CommitteeDto(
    Guid Id,
    Guid PublicationId,
    CommitteePaperDto? Paper,
    string Status,
    int MinApprovalsRequired,
    IReadOnlyList<CommitteeMemberDto> Members,
    /// <summary>Whose paper it is, so a listing can name the student.</summary>
    string? StudentName = null,
    /// <summary>False once it has finished: its decisions are what the coordinator ruled on.</summary>
    bool CanBeChanged = false)
{
    public int Approvals => Members.Count(m => m.Decision == CommitteeDecision.Approve);

    public int Decided => Members.Count(m => m.HasDecided);
}

public record CommitteeMemberReviewRequestDto(bool Approve, string Comments);

/// <param name="OverrideComposition">Set when this publication is to have a committee of a different shape from the one it was opened under. The API accepts it only with a reason in Comments.</param>
public record AssignCommitteeRequestDto(
    IReadOnlyList<Guid> MemberUserIds,
    int MinApprovalsRequired,
    string Comments,
    bool OverrideComposition = false);

/// <summary>Matches the backend's CommitteeStatus enum values.</summary>
public static class CommitteeStatus
{
    public const string Assigned = "Assigned";
    public const string InReview = "InReview";
    public const string Completed = "Completed";
}

/// <summary>The decision recorded against a committee member.</summary>
public static class CommitteeDecision
{
    public const string Pending = "Pending";
    public const string Approve = "Approve";
    public const string Reject = "Reject";
    public const string RequestRevision = "RequestRevision";
}

/// <summary>The paper a committee is judging, carried with the assignment itself.</summary>
public record CommitteePaperDto(
    Guid Id,
    string Title,
    string Abstract,
    int? PublicationYear,
    IReadOnlyList<string> Keywords,
    /// <summary>
    /// Whose paper it is. The assignment queue lets a member search and order by the student, so
    /// naming one is the least it owes them. Nothing here is anonymous review.
    /// </summary>
    string? StudentName = null);

/// <summary>
/// Somebody who could be put on a committee, as the API works it out.
/// <paramref name="IsExternal"/> decides which of the two required counts they fill.
/// </summary>
public record CommitteeCandidateDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyList<string> Roles,
    bool IsExternal)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}

/// <param name="MemberUserIds">The committee as it should now stand, not the people to add. Anyone left out is removed.</param>
public record UpdateCommitteeRequestDto(
    IReadOnlyList<Guid> MemberUserIds,
    int MinApprovalsRequired,
    string Comments,
    bool OverrideComposition = false);
