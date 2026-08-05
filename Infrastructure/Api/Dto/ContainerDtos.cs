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
    /// four separate waits: the supervisor reading it, an admin appointing a committee, the
    /// committee voting, the coordinator deciding, so the status alone cannot say. Carries
    /// RoleNames.EvaluationCommittee where the wait belongs to the committee rather than one role.
    /// </summary>
    string? PaperAwaitingRole = null,
    /// <summary>
    /// The evaluation committee this publication needs, as agreed when it was opened rather than
    /// as configured today. Null on publications created before the figures were recorded; the
    /// current settings govern those.
    /// </summary>
    int? RequiredReviewerMembers = null,
    int? RequiredExternalCommitteeMembers = null,
    /// <summary>
    /// Which ethics decision this is waiting for, by name. Finer than EthicsAwaitingRole, and the
    /// difference matters: two of the steps are the coordinator's and they are separate screens,
    /// so a role alone cannot say which one to send somebody to.
    /// </summary>
    string? EthicsAwaitingStep = null,
    /// <summary>
    /// True while the student is being asked to upload an ethics document again rather than for
    /// the first time. The stage says PendingUpload either way.
    /// </summary>
    bool EthicsDocumentsReturned = false,
    /// <summary>
    /// The student's department. Every appointment on a publication is scoped to it, so a screen
    /// that offers a choice of coordinator or head of department has to know which one applies.
    /// </summary>
    Guid? StudentDepartmentId = null,
    string? StudentDepartmentName = null,
    /// <summary>
    /// Which head of department the ethics decision was put to. Null before the stage reaches that
    /// step, and on institutions that do not run it.
    /// </summary>
    Guid? EthicsHeadOfDepartmentId = null,
    string? EthicsHeadOfDepartmentName = null)
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
    /// A publication whose ethics documents have been sent back for correction.
    ///
    /// It reads as ordinary work in progress otherwise, which is the one thing it is not: it is
    /// waiting on this student, now, and a student with six publications open could not tell
    /// which one the request was about.
    /// </summary>
    public bool NeedsEthicsDocumentsAgain => EthicsDocumentsReturned;

    /// <summary>
    /// Mirrors the backend rule in ContainerService.DeleteOwnAsync: a publication can only be
    /// discarded while it is still empty, so a student can undo one created by mistake. The backend
    /// enforces this independently. This only decides whether to offer the action.
    /// </summary>
    public bool CanBeDeleted => ProposalCount == 0 && CurrentPipeline == PipelineStage.ResearchProposals;

    /// <summary>
    /// What the user actually sees for this publication. Sorting and searching use it too, so
    /// the resulting order matches the labels on screen rather than the raw nullable Title.
    ///
    /// A publication takes its name from the proposal that goes ahead, so before one is chosen
    /// there is nothing to call it. "Untitled" read as something missing that somebody ought to
    /// fill in; this says what it is waiting for.
    /// </summary>
    public const string AwaitingTitle = "Awaiting an approved proposal";

    public string DisplayTitle =>
        string.IsNullOrWhiteSpace(Title) ? AwaitingTitle : Title!;
}

/// <summary>
/// One entry in a publication's narrative history: who did what, in what capacity, and the
/// comment that justified it. Every role with access to the publication sees the same log.
/// </summary>
public record ActivityHistoryEntryDto(
    Guid Id,
    string ActorName,
    /// <summary>The capacity the actor acted in: Coordinator, Supervisor, and so on.</summary>
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

/// <summary>What a publication's history can be filtered by: only what its own trail holds.</summary>
public record ActivityHistoryFiltersDto(
    IReadOnlyList<string> Actions,
    IReadOnlyList<ActivityHistoryActorDto> Actors);

public record ActivityHistoryActorDto(Guid UserId, string Name);

/// <summary>
/// Changing who is responsible for a publication already under way. Null leaves an assignment as
/// it is; the reason is required and stays on the publication's history.
/// </summary>
public record ReassignContainerRequestDto(
    Guid? CoordinatorUserId,
    Guid? SupervisorUserId,
    string Comments,
    Guid? HeadOfDepartmentUserId = null);
