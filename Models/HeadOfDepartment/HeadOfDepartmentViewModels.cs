using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// The Head of Department oversees a whole department rather than individual publications.
    /// Their one decision in the workflow is commenting on ethics documentation, so the dashboard
    /// separates that queue from the department's work as a whole.
    /// </summary>
    public class HeadOfDepartmentDashboardViewModel
    {
        public IReadOnlyList<PublicationContainerDto> Publications { get; set; } = [];

        public bool LoadFailed { get; set; }

        /// <summary>Publications whose ethics documentation is waiting on their comments.</summary>
        public IReadOnlyList<PublicationContainerDto> AwaitingReview =>
            Publications.Where(p => p.EthicsAwaitingRole == RoleNames.HeadOfDepartment).ToList();

        public int InProgress => Publications.Count(p => p.Status != "Completed");

        public int Completed => Publications.Count(p => p.Status == "Completed");
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
