using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// What every one of the supervisor's queues has in common: it is searched and ordered by the
    /// API, one page at a time, and it draws the same controls to say so.
    ///
    /// Shared rather than repeated three times because the screens are deliberately the same
    /// screen. A supervisor moving between proposals, ethics and papers should not have to learn
    /// where the search box went.
    /// </summary>
    public abstract class SupervisorQueue
    {
        public string? Sort { get; set; }
        public bool Descending { get; set; }
        public string? Search { get; set; }
        public int TotalCount { get; set; }
        public PagerViewModel? Pager { get; set; }
        public bool LoadFailed { get; set; }

        public bool HasSearch => !string.IsNullOrWhiteSpace(Search);

        protected abstract string QueueController { get; }
        protected abstract string QueueAction { get; }

        public Dictionary<string, string?> RouteValues()
        {
            var values = new Dictionary<string, string?>();
            if (!string.IsNullOrWhiteSpace(Sort)) values["sort"] = Sort;
            if (Descending) values["desc"] = "true";
            if (HasSearch) values["search"] = Search;
            return values;
        }

        /// <summary>
        /// Where Clear goes: this queue again, without the search term and still in the order the
        /// reader chose. Clearing a search should widen the list, not reorder what is left.
        /// </summary>
        public Dictionary<string, string?> ClearSearchRoute() =>
            RouteValues().Where(v => v.Key != "search").ToDictionary(v => v.Key, v => v.Value);

        public SortableColumnViewModel Column(string column, string label, bool descendingFirst = false) => new()
        {
            Controller = QueueController,
            Action = QueueAction,
            Column = column,
            Label = label,
            CurrentSort = Sort,
            CurrentDescending = Descending,
            DescendingFirst = descendingFirst,
            RouteValues = HasSearch ? new Dictionary<string, string?> { ["search"] = Search } : []
        };
    }

    /// <summary>
    /// A supervisor's overview: three figures for the three things that can be waiting on them,
    /// and below that everything they supervise.
    ///
    /// The listing is the one the coordinator's dashboard shows, for the same reason: the cards say
    /// how much is outstanding, and the listing says which publication each piece of it belongs to.
    /// It is paged and ordered by the API, so a supervisor with sixty publications reads them in
    /// whatever order they ask for rather than the order they happened to be created in.
    /// </summary>
    public class SupervisorDashboardViewModel : SupervisorQueue
    {
        protected override string QueueController => "Supervisor";
        protected override string QueueAction => "SupervisorDashboard";

        /// <summary>Everything they supervise, one page of it.</summary>
        public IReadOnlyList<PublicationContainerDto> Supervising { get; set; } = [];

        /// <summary>Proposals a coordinator has sent them, still awaiting an answer.</summary>
        public int ProposalsToReviewTotal { get; set; }

        /// <summary>
        /// Ethics waiting on them, both kinds together: the ruling on whether approval is needed at
        /// all, and the check of the documents once it is. One card, because they are one queue,
        /// with the split named underneath because the two read differently.
        /// </summary>
        public int EthicsAwaitingRulingTotal { get; set; }
        public int EthicsAwaitingCheckTotal { get; set; }

        public int EthicsTotal => EthicsAwaitingRulingTotal + EthicsAwaitingCheckTotal;

        /// <summary>Research papers submitted for their review.</summary>
        public int PapersToReviewTotal { get; set; }

        public int ActionsWaiting => ProposalsToReviewTotal + EthicsTotal + PapersToReviewTotal;
    }

    /// <summary>Proposals sent to this supervisor to choose from.</summary>
    public class InvitedProposalsViewModel : SupervisorQueue
    {
        protected override string QueueController => "Supervisor";
        protected override string QueueAction => "proposal_review";

        public IReadOnlyList<ProposalDto> Proposals { get; set; } = [];
    }

    /// <summary>
    /// Every ethics stage waiting on this supervisor, whichever of the two decisions it wants.
    ///
    /// One queue rather than two screens: the question being asked is "what is mine to do", and
    /// splitting it by which kind of ethics work it happens to be would mean looking in two places
    /// to find out. Each row says which decision it is waiting for and opens the screen that asks
    /// for it.
    /// </summary>
    public class SupervisorEthicsQueueViewModel : SupervisorQueue
    {
        protected override string QueueController => "Supervisor";
        protected override string QueueAction => "Ethic_reviews";

        public IReadOnlyList<PublicationContainerDto> Items { get; set; } = [];

        /// <summary>Whether this row wants the ruling rather than the document check.</summary>
        public static bool IsRuling(PublicationContainerDto container) =>
            container.EthicsAwaitingStep == EthicsSteps.SupervisorDecision;
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
    public class SupervisorPapersViewModel : SupervisorQueue
    {
        protected override string QueueController => "Supervisor";
        protected override string QueueAction => "publication_review";

        public IReadOnlyList<PublicationDto> Papers { get; set; } = [];
    }
}
