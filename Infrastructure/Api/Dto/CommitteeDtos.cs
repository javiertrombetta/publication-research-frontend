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
    string Status,
    int MinApprovalsRequired,
    IReadOnlyList<CommitteeMemberDto> Members)
{
    public int Approvals => Members.Count(m => m.Decision == CommitteeDecision.Approve);

    public int Decided => Members.Count(m => m.HasDecided);
}

public record CommitteeMemberReviewRequestDto(bool Approve, string Comments);

public record AssignCommitteeRequestDto(
    IReadOnlyList<Guid> MemberUserIds,
    int MinApprovalsRequired,
    string Comments);

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
