namespace ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

public record PublicationContainerDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid CoordinatorId,
    string CoordinatorName,
    Guid? AssignedSupervisorId,
    string? AssignedSupervisorName,
    int CurrentPipeline,
    string Status,
    DateTime CreatedAt,
    /// <summary>Paper title once one exists, else the approved proposal's title; null while still drafting proposals.</summary>
    string? Title,
    /// <summary>Proposals held by this publication. Zero is the only point at which it can be discarded.</summary>
    int ProposalCount,
    /// <summary>
    /// The research paper's own status, null until a paper exists. Status only says InProgress or
    /// Completed, which can't distinguish an accepted paper from one still under review.
    /// </summary>
    string? PaperStatus = null,
    /// <summary>
    /// The ethics approval's status, null before the student has declared. Lets a listing show
    /// what a publication is waiting on without a request per row.
    /// </summary>
    string? EthicsStatus = null,
    /// <summary>
    /// Whose turn it is in the ethics workflow, as a role name, or null when nothing is pending.
    /// EthicsStatus can't answer this on its own: PendingVerification covers four different
    /// waits, told apart on the backend by which timestamps have been set.
    /// </summary>
    string? EthicsAwaitingRole = null,
    /// <summary>
    /// Whose turn it is on the research paper, or null when nothing is pending. UnderReview covers
    /// four separate waits — the supervisor reading it, an admin appointing a committee, the
    /// committee voting, the coordinator deciding — so the status alone cannot say. Carries
    /// RoleNames.EvaluationCommittee where the wait belongs to the committee rather than one role.
    /// </summary>
    string? PaperAwaitingRole = null,
    /// <summary>
    /// The evaluation committee this publication needs, as agreed when it was opened rather than
    /// as configured today. Null on publications created before the figures were recorded; the
    /// current settings govern those.
    /// </summary>
    int? RequiredInternalCommitteeMembers = null,
    int? RequiredExternalCommitteeMembers = null)
{
    /// <summary>
    /// True once the paper has cleared review, whether or not its author has yet decided to put
    /// it in the public catalogue. The publication stage is finished either way.
    /// </summary>
    public bool IsPaperDecided =>
        PaperStatus is PublicationStatus.Accepted or PublicationStatus.Published;

    /// <summary>
    /// What to show as this publication's status. Falls back to the container's own status while
    /// there is no paper, and prefers the paper's once there is one, since that is the specific
    /// thing the student is waiting on.
    /// </summary>
    public string DisplayStatus =>
        Status == "Completed" || PaperStatus is null ? Status : PaperStatus;

    /// <summary>
    /// Mirrors the backend rule in ContainerService.DeleteOwnAsync: a publication can only be
    /// discarded while it is still empty, so a student can undo one created by mistake.
    /// The backend enforces this independently — this only decides whether to offer the action.
    /// </summary>
    public bool CanBeDeleted => ProposalCount == 0 && CurrentPipeline == PipelineStage.ResearchProposals;

    /// <summary>
    /// What the user actually sees for this publication. Sorting and searching use it too, so
    /// the resulting order matches the labels on screen rather than the raw nullable Title.
    /// </summary>
    public string DisplayTitle =>
        string.IsNullOrWhiteSpace(Title) ? "Untitled publication" : Title!;
}

/// <summary>
/// One entry in a publication's narrative history: who did what, in what capacity, and the
/// comment that justified it. Every role with access to the publication sees the same log.
/// </summary>
public record ActivityHistoryEntryDto(
    Guid Id,
    string ActorName,
    /// <summary>The capacity the actor acted in — Coordinator, Supervisor, and so on.</summary>
    string? ActorRole,
    string? OnBehalfOfName,
    string Action,
    string Comments,
    string? PreviousStatus,
    string? NewStatus,
    DateTime CreatedAt);

/// <summary>Pipeline stages as returned in PublicationContainerDto.CurrentPipeline.</summary>
public static class PipelineStage
{
    public const int ResearchProposals = 1;
    public const int EthicsApproval = 2;
    public const int ResearchPaper = 3;
}
