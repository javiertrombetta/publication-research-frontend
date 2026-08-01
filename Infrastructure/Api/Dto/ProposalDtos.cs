namespace ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

public record ProposalDto(
    Guid Id,
    Guid PublicationContainerId,
    string Title,
    string Abstract,
    string Status,
    DateTime? SubmittedAt);

public record SaveProposalRequestDto(string Title, string Abstract);

/// <summary>A Coordinator inviting supervisors to consider a set of proposals.</summary>
public record SendToSupervisorsRequestDto(
    IReadOnlyList<Guid> ProposalIds,
    IReadOnlyList<Guid> SupervisorIds,
    string Comments);

public record AssignSupervisorRequestDto(Guid SupervisorId, string Comments);

/// <summary>
/// A supervisor invited to a proposal. IsSelected is their answer: true once they have said they
/// are willing to supervise it, which is what a Coordinator waits for before assigning.
/// </summary>
public record SupervisorInvitationDto(
    Guid ProposalId,
    Guid SupervisorId,
    string SupervisorName,
    bool IsSelected,
    string? Comments,
    DateTime InvitedAt,
    DateTime? SelectedAt);

/// <summary>Matches the backend's ProposalStatus enum values.</summary>
public static class ProposalStatus
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string UnderSupervisorReview = "UnderSupervisorReview";
    public const string Assigned = "Assigned";
    public const string Rejected = "Rejected";
    public const string DeferredToNextCycle = "DeferredToNextCycle";
}

/// <summary>A supervisor saying they are willing to take a proposal on. Comments are optional.</summary>
public record SupervisorSelectionRequestDto(string? Comments);

/// <summary>
/// A proposal with the supervisors it went to and what each said. One request for a whole
/// screen's worth, instead of one per proposal.
/// </summary>
public record ProposalWithInvitationsDto(
    Guid Id,
    Guid PublicationContainerId,
    string StudentName,
    string Title,
    string Abstract,
    string Status,
    DateTime? SubmittedAt,
    IReadOnlyList<SupervisorInvitationDto> Invitations,
    /// <summary>
    /// When this proposal was last put back in the dispatch queue after a round that found nobody
    /// willing, and null if it has never been. A proposal waiting its first turn and one that has
    /// already had one are different things to decide about.
    /// </summary>
    DateTime? ReturnedToDispatchAt = null)
{
    public bool CameBack => ReturnedToDispatchAt is not null;
}

/// <summary>What discarding a set of offers actually did.</summary>
public record DiscardSelectionsResultDto(string StudentName, int ProposalsReturned, bool StudentHasNothingLeft);

/// <summary>How much of the dispatch queue is there for a second time, over the whole queue.</summary>
public record ReturnedToDispatchSummaryDto(int Students, int Proposals);
