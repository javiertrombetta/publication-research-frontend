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
    /// Ethics documentation awaiting the Head of Department's comments.
    ///
    /// The same queue shape as the department's other listings, so it is searched, ordered and
    /// paged by the API rather than in the page already in hand. One of these is only ever a
    /// handful of rows on a quiet week and the whole department's backlog on a busy one, and the
    /// screen cannot tell which it is going to be.
    /// </summary>
    public class HeadOfDepartmentEthicsViewModel : DepartmentQueue
    {
        protected override string QueueAction => "Headofdepartment_feedback";

        /// <summary>
        /// Narrowed to one publication, when the reader arrived from a link on the dashboard. The
        /// sort and search controls are pointless on a queue of one, so the screen hides them.
        /// </summary>
        public Guid? OnlyId { get; set; }

        public List<HeadOfDepartmentEthicsItem> Items { get; set; } = [];
    }

    public class HeadOfDepartmentEthicsItem
    {
        public PublicationContainerDto Container { get; set; } = null!;

        public EthicsApprovalDto Approval { get; set; } = null!;

        public IReadOnlyList<EthicsDocumentDto> Documents { get; set; } = [];
    }

}
