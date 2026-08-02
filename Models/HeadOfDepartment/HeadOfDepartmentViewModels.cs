using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// The Head of Department oversees a whole department rather than individual publications.
    /// Their one decision in the workflow is commenting on ethics documentation, so the dashboard
    /// separates that queue from the department's work as a whole.
    /// </summary>
    /// <summary>
    /// What every one of the head of department's listings has in common: searched and ordered by
    /// the API, one page at a time, with the same controls saying so. The same shape the
    /// coordinator's and the supervisor's screens use, because they are the same kind of screen.
    /// </summary>
    public abstract class DepartmentQueue
    {
        public string? Sort { get; set; }
        public bool Descending { get; set; }
        public string? Search { get; set; }
        public int TotalCount { get; set; }
        public PagerViewModel? Pager { get; set; }
        public bool LoadFailed { get; set; }

        public bool HasSearch => !string.IsNullOrWhiteSpace(Search);

        protected abstract string QueueAction { get; }

        public Dictionary<string, string?> RouteValues()
        {
            var values = new Dictionary<string, string?>();
            if (!string.IsNullOrWhiteSpace(Sort)) values["sort"] = Sort;
            if (Descending) values["desc"] = "true";
            if (HasSearch) values["search"] = Search;
            return values;
        }

        public Dictionary<string, string?> ClearSearchRoute() =>
            RouteValues().Where(v => v.Key != "search").ToDictionary(v => v.Key, v => v.Value);

        public SortableColumnViewModel Column(string column, string label, bool descendingFirst = false) => new()
        {
            Controller = "HeadOfDepartment",
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
    /// The department at a glance: how much sits at each of the three stages, and the listing of
    /// everything moving through them.
    ///
    /// A head of department is not an administrator, and this is not the institution's dashboard.
    /// It is the authority over one department, which is what makes overseeing every stage of it
    /// theirs to do even though only the ethics comment is theirs to decide. So the listing names
    /// whoever the next move belongs to rather than only the rows waiting on them.
    /// </summary>
    public class HeadOfDepartmentDashboardViewModel : DepartmentQueue
    {
        protected override string QueueAction => "Head_of_Department_dashboard";

        public IReadOnlyList<PublicationContainerDto> Publications { get; set; } = [];

        /// <summary>How much of the department sits at each stage. Totals, not this page.</summary>
        public int ProposalStageTotal { get; set; }
        public int EthicsStageTotal { get; set; }
        public int PaperStageTotal { get; set; }

        /// <summary>The one decision that is theirs, so the card can say it is waiting.</summary>
        public int AwaitingMyReviewTotal { get; set; }
    }

    /// <summary>
    /// The research paper stage across the department, read-only. Their decisions are elsewhere;
    /// this answers what is happening and who is holding it up.
    /// </summary>
    public class DepartmentPapersViewModel : DepartmentQueue
    {
        protected override string QueueAction => "Department_papers";

        public IReadOnlyList<PublicationContainerDto> Publications { get; set; } = [];
    }

    /// <summary>Ethics documentation awaiting the Head of Department's comments.</summary>
    public class HeadOfDepartmentEthicsViewModel : SortablePublicationQueue
    {
        protected override string SortController => "HeadOfDepartment";
        protected override string SortAction => "Headofdepartment_feedback";


        /// <summary>
        /// Which page of the queue this is. Null where everything fits on one, so the controls
        /// only appear when there is somewhere to go.
        /// </summary>
        public PagerViewModel? Pager { get; set; }
        public List<HeadOfDepartmentEthicsItem> Items { get; set; } = [];

        public bool LoadFailed { get; set; }
    }

    public class HeadOfDepartmentEthicsItem
    {
        public PublicationContainerDto Container { get; set; } = null!;

        public EthicsApprovalDto Approval { get; set; } = null!;

        public IReadOnlyList<EthicsDocumentDto> Documents { get; set; } = [];
    }

    /// <summary>Every proposal from students in the department, for oversight rather than action.</summary>
    public class DepartmentProposalsViewModel
    {
        /// <summary>
        /// The order and the search term, both applied by the API before the page is cut. A
        /// department's oldest proposal is on its last page, so ordering what has already arrived
        /// would never bring it into view.
        /// </summary>
        public string? Sort { get; set; }
        public bool Descending { get; set; }
        public string? Search { get; set; }
        public int TotalCount { get; set; }

        public bool HasSearch => !string.IsNullOrWhiteSpace(Search);

        public Dictionary<string, string?> RouteValues()
        {
            var values = new Dictionary<string, string?>();
            if (HasSearch) values["search"] = Search;
            if (!string.IsNullOrWhiteSpace(Sort)) values["sort"] = Sort;
            if (Descending) values["desc"] = "true";
            return values;
        }

        /// <summary>
        /// Where Clear goes: this list again, without the search term and still in the order the
        /// reader had chosen.
        /// </summary>
        public Dictionary<string, string?> ClearSearchRoute() =>
            RouteValues().Where(v => v.Key != "search").ToDictionary(v => v.Key, v => v.Value);

        public SortBarViewModel SortBar => new()
        {
            Controller = "HeadOfDepartment",
            Action = "all_proposals_fromstudent",
            Sort = Sort,
            Descending = Descending,
            RouteValues = HasSearch ? new Dictionary<string, string?> { ["search"] = Search } : [],
            Columns =
            [
                ("submitted", "Date", true),
                ("student", "Student", false),
                ("title", "Proposal", false),
                ("status", "Status", false)
            ]
        };


        /// <summary>
        /// Which page of the queue this is. Null where everything fits on one, so the controls
        /// only appear when there is somewhere to go.
        /// </summary>
        public PagerViewModel? Pager { get; set; }
        public List<DepartmentProposalItem> Items { get; set; } = [];

        public bool LoadFailed { get; set; }
    }

    public class DepartmentProposalItem
    {
        /// <summary>Carried on the proposals themselves, so the screen is one request.</summary>
        public string StudentName { get; set; } = string.Empty;

        public IReadOnlyList<ProposalDto> Proposals { get; set; } = [];
    }
}
