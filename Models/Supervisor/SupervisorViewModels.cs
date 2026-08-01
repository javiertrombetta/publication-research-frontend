using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// A supervisor's queue. Everything they do is a decision someone else is waiting on, so the
    /// dashboard is organised by decision rather than by publication.
    /// </summary>
    public class SupervisorDashboardViewModel
    {
        /// <summary>Proposals a coordinator has sent them, still awaiting a yes.</summary>
        public IReadOnlyList<ProposalDto> InvitedProposals { get; set; } = [];

        /// <summary>Publications where the student has declared and nobody has ruled yet.</summary>
        public IReadOnlyList<PublicationContainerDto> EthicsAwaitingDecision { get; set; } = [];

        /// <summary>Publications whose ethics documents have been uploaded and need checking.</summary>
        public IReadOnlyList<PublicationContainerDto> EthicsAwaitingReview { get; set; } = [];

        /// <summary>Research papers submitted for this supervisor's review.</summary>
        public IReadOnlyList<PublicationDto> PapersAwaitingReview { get; set; } = [];

        /// <summary>How many there are altogether. The dashboard states a figure, not a page.</summary>
        public int PapersAwaitingReviewTotal { get; set; }

        /// <summary>Everything they supervise, whether or not it needs them right now.</summary>
        public IReadOnlyList<PublicationContainerDto> Supervising { get; set; } = [];

        public bool LoadFailed { get; set; }

        public int ActionsWaiting =>
            InvitedProposals.Count + EthicsAwaitingDecision.Count +
            EthicsAwaitingReview.Count + PapersAwaitingReview.Count;
    }

    /// <summary>Proposals sent to this supervisor to choose from.</summary>
    public class InvitedProposalsViewModel
    {
        public IReadOnlyList<ProposalDto> Proposals { get; set; } = [];

        public bool LoadFailed { get; set; }
    }

    /// <summary>
    /// One publication's ethics stage as the supervisor sees it: the student's declaration, and
    /// the documents once they exist.
    /// </summary>
    public class SupervisorEthicsViewModel
    {
        public PublicationContainerDto Container { get; set; } = null!;

        public EthicsApprovalDto? Approval { get; set; }

        public IReadOnlyList<EthicsDocumentDto> Documents { get; set; } = [];

        /// <summary>True when the supervisor still has to rule on whether documentation is needed.</summary>
        public bool NeedsRequirementDecision =>
            Approval?.Status == EthicsStatus.PendingSupervisorDecision;

        /// <summary>
        /// True only while a document is still awaiting its first review. Once the supervisor
        /// has accepted them the status is unchanged but the work is the coordinator's.
        /// </summary>
        public bool NeedsDocumentReview =>
            Documents.Any(d => d.Status == EthicsDocumentStatus.PendingReview);
    }

    /// <summary>Papers waiting on this supervisor's review.</summary>
    public class SupervisorPapersViewModel
    {

        /// <summary>
        /// Which page of the queue this is. Null where everything fits on one, so the controls
        /// only appear when there is somewhere to go.
        /// </summary>
        public PagerViewModel? Pager { get; set; }
        public IReadOnlyList<PublicationDto> Papers { get; set; } = [];

        public bool LoadFailed { get; set; }
    }
}
