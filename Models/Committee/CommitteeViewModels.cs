using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// A committee member's assignments. Their whole job is one decision per paper, so the
    /// dashboard is split by whether that decision has been made.
    /// </summary>
    public class CommitteeDashboardViewModel
    {

        /// <summary>
        /// Which page of the queue this is. Null where everything fits on one, so the controls
        /// only appear when there is somewhere to go.
        /// </summary>
        public PagerViewModel? Pager { get; set; }
        public List<CommitteeAssignmentItem> Items { get; set; } = [];

        public bool LoadFailed { get; set; }

        /// <summary>
        /// How the queue is searched and ordered, both applied by the API. The same controls every
        /// other reviewer queue has: this was the one screen without them, and it is now reachable
        /// by anybody an administrator can appoint rather than only the two committee roles.
        /// </summary>
        public string? Sort { get; set; }
        public bool Descending { get; set; }
        public string? Search { get; set; }
        public int TotalCount { get; set; }

        public bool HasSearch => !string.IsNullOrWhiteSpace(Search);

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
            Controller = "ExternalSupervisor",
            Action = "committee_review",
            Column = column,
            Label = label,
            CurrentSort = Sort,
            CurrentDescending = Descending,
            DescendingFirst = descendingFirst,
            RouteValues = HasSearch ? new Dictionary<string, string?> { ["search"] = Search } : []
        };

        public IReadOnlyList<CommitteeAssignmentItem> AwaitingMe =>
            Items.Where(i => !i.HasDecided).ToList();

        public IReadOnlyList<CommitteeAssignmentItem> Decided =>
            Items.Where(i => i.HasDecided).ToList();
    }

    /// <summary>One paper this member has been asked to evaluate.</summary>
    public class CommitteeAssignmentItem
    {
        public CommitteeDto Committee { get; set; } = null!;

        /// <summary>Null if the paper could not be read. The row is still worth showing.</summary>
        public CommitteePaperDto? Paper => Committee.Paper;

        /// <summary>This member's own place on the committee.</summary>
        public CommitteeMemberDto? Me { get; set; }

        public bool HasDecided => Me?.HasDecided == true;

        public string Title => Paper?.Title is { Length: > 0 } title ? title : "Untitled paper";
    }
}
