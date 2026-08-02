namespace ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

public record ProposalDto(
    Guid Id,
    Guid PublicationContainerId,
    string Title,
    string Abstract,
    string Status,
    DateTime? SubmittedAt,
    /// <summary>
    /// When the supervisor reading this has to have answered by. Only filled in on the listing of
    /// proposals sent to a supervisor, which is the only place anybody is being held to it.
    /// </summary>
    DateTime? RespondBy = null)
{
    /// <summary>The date in the reader's own time, which is the only one worth showing them.</summary>
    public DateTime? RespondByLocal => RespondBy?.ToLocalTime();

    public bool RespondByHasPassed => RespondByLocal is { } by && by < DateTime.Now;
}

public record SaveProposalRequestDto(string Title, string Abstract);

/// <summary>A Coordinator inviting supervisors to consider a set of proposals.</summary>
public record SendToSupervisorsRequestDto(
    IReadOnlyList<Guid> ProposalIds,
    IReadOnlyList<Guid> SupervisorIds,
    string Comments,
    /// <summary>
    /// When the supervisors have to answer by. The API requires it: a round with no date never
    /// ends. Once it passes, students with no proposal anybody offered to take on go back to the
    /// dispatch queue on their own.
    /// </summary>
    DateTime? RespondBy = null);

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
    DateTime? SelectedAt,
    /// <summary>When this round has to be answered by, and null where the coordinator set no date.</summary>
    DateTime? RespondBy = null);

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
    DateTime? ReturnedToDispatchAt = null,
    /// <summary>
    /// The student's institutional id, or null on an account with no student profile. A queue
    /// grouped by student heads one group per publication, so the same name appears twice for
    /// anybody with two open, and two people can share a name outright.
    /// </summary>
    string? StudentIdNumber = null,
    /// <summary>
    /// The address on the student's account, which at this institution is their id at the student
    /// domain. Carried rather than composed from the id, because it is the address that reaches
    /// them.
    /// </summary>
    string? StudentEmail = null)
{
    public bool CameBack => ReturnedToDispatchAt is not null;
}

/// <summary>What discarding a set of offers actually did.</summary>
public record DiscardSelectionsResultDto(string StudentName, int ProposalsReturned, bool StudentHasNothingLeft);

/// <summary>
/// What the dispatch screen needs beyond its page of proposals: how much of the queue is there for
/// the second time, and the answer-by date to offer for the next send.
/// </summary>
/// <param name="SuggestedRespondBy">
/// Now plus the institution's expected supervisor response time, in UTC. A starting point the
/// coordinator can move, not a rule.
/// </param>
public record ReturnedToDispatchSummaryDto(int Students, int Proposals, DateTime SuggestedRespondBy);
