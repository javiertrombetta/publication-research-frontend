using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// The publications waiting on the coordinator at one point of the ethics workflow. Both of the
    /// coordinator's ethics screens use this. What differs is which decision each offers.
    /// </summary>
    public class CoordinatorEthicsViewModel
    {

        /// <summary>
        /// Which page of the queue this is. Null where everything fits on one, so the controls
        /// only appear when there is somewhere to go.
        /// </summary>
        public PagerViewModel? Pager { get; set; }
        public List<CoordinatorEthicsItem> Items { get; set; } = [];

        /// <summary>Which of the coordinator's two ethics steps this screen is showing.</summary>
        public string Stage { get; set; } = string.Empty;

        public bool LoadFailed { get; set; }
    }

    public class CoordinatorEthicsItem
    {
        public PublicationContainerDto Container { get; set; } = null!;

        public EthicsApprovalDto Approval { get; set; } = null!;

        public IReadOnlyList<EthicsDocumentDto> Documents { get; set; } = [];

        /// <summary>
        /// True when a supervisor found no documentation necessary and the coordinator is being
        /// asked to confirm or override that, rather than to review uploaded documents.
        /// </summary>
        public bool IsNotRequiredConfirmation =>
            Approval.Status == EthicsStatus.NotRequired;
    }

    /// <summary>Research papers awaiting the coordinator's final decision.</summary>
    /// <summary>
    /// The coordinator's view of the research paper stage, in two parts.
    ///
    /// Papers they can act on are separated from papers that are merely moving, because those are
    /// different things to a person and the screen used to treat them as one. It listed everything
    /// whose paper was under review and offered the same decision form on each, but the API refuses
    /// that decision until the evaluation committee has finished, so most of the buttons returned
    /// an error. Hiding the rest would be worse: a paper stuck for three weeks with a committee
    /// that has not voted is precisely the one a coordinator needs to see, and it is theirs to
    /// chase. So both are shown, and only one of them has a form.
    /// </summary>
    public class CoordinatorPapersViewModel
    {
        /// <summary>Waiting on the coordinator, and nobody else.</summary>
        public List<CoordinatorPaperItem> ReadyForDecision { get; set; } = [];

        /// <summary>Moving, but not theirs to move. Read-only, and says whose turn it is.</summary>
        public List<CoordinatorPaperInProgress> InProgress { get; set; } = [];

        public bool LoadFailed { get; set; }

        public bool IsEmpty => ReadyForDecision.Count == 0 && InProgress.Count == 0;
    }

    /// <summary>
    /// A paper somewhere else in the review process. Everything shown here comes from the
    /// containers listing, so a row nobody can act on costs no further requests.
    /// </summary>
    public class CoordinatorPaperInProgress
    {
        public PublicationContainerDto Container { get; set; } = null!;

        /// <summary>
        /// What the paper is actually waiting for, in words. Derived from the role the API says
        /// the turn belongs to rather than from the status, which cannot tell these apart.
        /// </summary>
        public string WaitingOn => Container.PaperAwaitingRole switch
        {
            Common.RoleNames.Supervisor => "With the supervisor, who has yet to review it",
            Common.RoleNames.Admin => "Waiting for an evaluation committee to be appointed",
            Common.RoleNames.EvaluationCommittee => "The evaluation committee has not finished voting",
            Common.RoleNames.Student => "With the author, who is revising it",
            _ => "In progress"
        };
    }

    public class CoordinatorPaperItem
    {
        public PublicationContainerDto Container { get; set; } = null!;

        public PublicationDto Paper { get; set; } = null!;

        /// <summary>Every review recorded so far: the supervisor's and the committee's.</summary>
        public IReadOnlyList<ReviewDto> Reviews { get; set; } = [];
    }
}
