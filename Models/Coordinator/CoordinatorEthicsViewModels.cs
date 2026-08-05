using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// The publications waiting on the coordinator at one point of the ethics workflow. Both of the
    /// coordinator's ethics screens use this. What differs is which decision each offers.
    /// </summary>
    public class CoordinatorEthicsViewModel : SortablePublicationQueue
    {
        /// <summary>Which of the coordinator's two ethics queues this is, so the bar links back here.</summary>
        protected override string SortController => "Coordinator";

        // Matched on the name the controller's own enum produces, which is what Stage carries.
        protected override string SortAction =>
            Stage == "AfterHeadOfDepartment"
                ? "Ethic_review_afters_headofdepartment"
                : "Ethic_review_aftersupervisor";


        /// <summary>
        /// Which page of the queue this is. Null where everything fits on one, so the controls
        /// only appear when there is somewhere to go.
        /// </summary>
        public PagerViewModel? Pager { get; set; }
        public List<CoordinatorEthicsItem> Items { get; set; } = [];

        /// <summary>Which of the coordinator's two ethics steps this screen is showing.</summary>
        public string Stage { get; set; } = string.Empty;

        public bool IsFinalDecision => Stage == "AfterHeadOfDepartment";

        /// <summary>
        /// How much is waiting on the coordinator's other ethics screen.
        ///
        /// The menu has one Ethics decisions entry and the coordinator has two ethics queues, so
        /// work sitting in the other one could go unseen for as long as nobody thought to look at
        /// the dashboard. Each screen says when the other has something on it.
        /// </summary>
        public int OtherQueueCount { get; set; }

        public string OtherQueueAction =>
            IsFinalDecision ? "Ethic_review_aftersupervisor" : "Ethic_review_afters_headofdepartment";

        public string OtherQueueName =>
            IsFinalDecision ? "Ethics awaiting you" : "Final ethics decision";

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

        /// <summary>
        /// Two lists, each its own page.
        ///
        /// They used to be one request split in the controller, which meant neither could be paged:
        /// a page of publications holds any number of rows for either list, or none, so "page 2"
        /// answered nothing. The API is asked for each list separately now, by whose turn it is, so
        /// a page of either is a stable page of that list. Different query keys for the same
        /// reason: turning one must not turn the other.
        /// </summary>
        public PagerViewModel? DecisionPager { get; set; }
        public PagerViewModel? ProgressPager { get; set; }

        public int DecisionTotal { get; set; }
        public int ProgressTotal { get; set; }

        public string? Sort { get; set; }
        public bool Descending { get; set; }
        public string? Search { get; set; }

        public string? ProgressSort { get; set; }
        public bool ProgressDescending { get; set; }
        public string? ProgressSearch { get; set; }

        public bool HasSearch => !string.IsNullOrWhiteSpace(Search);
        public bool HasProgressSearch => !string.IsNullOrWhiteSpace(ProgressSearch);

        /// <summary>Everything both lists' links have to carry, or one resets the other.</summary>
        public Dictionary<string, string?> RouteValues(bool includeDecisionPage = true, bool includeProgressPage = true)
        {
            var values = new Dictionary<string, string?>();
            if (!string.IsNullOrWhiteSpace(Sort)) values["sort"] = Sort;
            if (Descending) values["desc"] = "true";
            if (HasSearch) values["search"] = Search;
            if (!string.IsNullOrWhiteSpace(ProgressSort)) values["progressSort"] = ProgressSort;
            if (ProgressDescending) values["progressDesc"] = "true";
            if (HasProgressSearch) values["progressSearch"] = ProgressSearch;
            return values;
        }

        /// <summary>
        /// Where each list's Clear goes. Only that list's own term is dropped: the two are read
        /// independently, so clearing one must leave the other narrowed as it was, and both keep
        /// the order they were in.
        /// </summary>
        public Dictionary<string, string?> ClearDecisionSearchRoute() =>
            RouteValues().Where(v => v.Key != "search").ToDictionary(v => v.Key, v => v.Value);

        public Dictionary<string, string?> ClearProgressSearchRoute() =>
            RouteValues().Where(v => v.Key != "progressSearch").ToDictionary(v => v.Key, v => v.Value);

        /// <summary><inheritdoc cref="ProgressColumn" path="/summary"/></summary>
        public SortableColumnViewModel Column(string column, string label, bool descendingFirst = false) => new()
        {
            Controller = "Coordinator",
            Action = "Evaluation_after_committee",
            Column = column,
            Label = label,
            CurrentSort = Sort,
            CurrentDescending = Descending,
            DescendingFirst = descendingFirst,
            RouteValues = RouteValues().Where(v => v.Key is not ("sort" or "desc"))
                .ToDictionary(v => v.Key, v => v.Value)
        };

        /// <summary>
        /// One sortable heading for the second listing, which is a table rather than a stack of
        /// cards, so its headings sit in the table's own head. Its own query keys, or ordering one
        /// of the two listings would reorder the other with it.
        /// </summary>
        public SortableColumnViewModel ProgressColumn(string column, string label, bool descendingFirst = false) => new()
        {
            Controller = "Coordinator",
            Action = "Evaluation_after_committee",
            Column = column,
            Label = label,
            CurrentSort = ProgressSort,
            CurrentDescending = ProgressDescending,
            DescendingFirst = descendingFirst,
            SortKey = "progressSort",
            DescendingKey = "progressDesc",
            RouteValues = RouteValues().Where(v => v.Key is not ("progressSort" or "progressDesc"))
                .ToDictionary(v => v.Key, v => v.Value)
        };
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
