using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// Submitted proposals that no supervisor has been invited to yet, and the supervisors they
    /// can be sent to.
    /// </summary>
    public class AssignProposalsViewModel
    {

        /// <summary>
        /// Which page of the queue this is. Null where everything fits on one, so the controls
        /// only appear when there is somewhere to go.
        /// </summary>
        public PagerViewModel? Pager { get; set; }
        /// <summary>
        /// Each proposal carries its student's name, so the screen needs nothing else to head a
        /// group. It used to be matched against a separate page of the coordinator's publications,
        /// which meant any student past the first page of that lookup was headed "Unknown student".
        /// </summary>
        public IReadOnlyList<ProposalWithInvitationsDto> Proposals { get; set; } = [];

        public IReadOnlyList<UserListItemDto> Supervisors { get; set; } = [];

        public bool LoadFailed { get; set; }

        public string StudentFor(Guid containerId) =>
            Proposals.FirstOrDefault(p => p.PublicationContainerId == containerId)?.StudentName
            ?? "Unknown student";

        /// <summary>Proposals grouped by student, since they are sent out per student.</summary>
        public IEnumerable<IGrouping<Guid, ProposalWithInvitationsDto>> ByPublication =>
            Proposals.GroupBy(p => p.PublicationContainerId);
    }

    /// <summary>Proposals a supervisor has offered to take on, awaiting the coordinator.</summary>
    public class SupervisorSelectionsViewModel
    {

        /// <summary>
        /// Which page of the queue this is. Null where everything fits on one, so the controls
        /// only appear when there is somewhere to go.
        /// </summary>
        public PagerViewModel? Pager { get; set; }
        public List<SupervisorSelectionItem> Items { get; set; } = [];

        public bool LoadFailed { get; set; }

        /// <summary>
        /// What the coordinator typed, and how the list is ordered. Both travel in the query string
        /// and both are applied by the API, before the page is cut. Searching or sorting the ten
        /// rows already fetched would answer a different question from the one being asked: the
        /// proposal somebody is looking for is usually the one not on this page.
        /// </summary>
        public string? Search { get; set; }
        public string? Sort { get; set; }
        public bool Descending { get; set; }

        /// <summary>How many matched altogether, which is what a search result is judged by.</summary>
        public int TotalCount { get; set; }

        public bool HasSearch => !string.IsNullOrWhiteSpace(Search);

        /// <summary>The state every sort link and page link has to carry, or it drops the search.</summary>
        public Dictionary<string, string?> RouteValues()
        {
            var values = new Dictionary<string, string?>();
            if (HasSearch) values["search"] = Search;
            if (!string.IsNullOrWhiteSpace(Sort)) values["sort"] = Sort;
            if (Descending) values["desc"] = "true";
            return values;
        }

        public SortableColumnViewModel Column(string column, string label, bool descendingFirst = false) => new()
        {
            Controller = "Coordinator",
            Action = "select_a_proposal_forstudent",
            Column = column,
            Label = label,
            CurrentSort = Sort,
            CurrentDescending = Descending,
            DescendingFirst = descendingFirst,
            RouteValues = HasSearch ? new Dictionary<string, string?> { ["search"] = Search } : []
        };
    }

    public class SupervisorSelectionItem
    {
        /// <summary>Carried on the proposal itself, so the screen needs no second request to name the author.</summary>
        public string StudentName { get; set; } = string.Empty;

        public ProposalDto Proposal { get; set; } = null!;

        public IReadOnlyList<SupervisorInvitationDto> Invitations { get; set; } = [];

        /// <summary>The supervisors who said yes, the only ones who can actually be assigned.</summary>
        public IEnumerable<SupervisorInvitationDto> Willing => Invitations.Where(i => i.IsSelected);

        public IEnumerable<SupervisorInvitationDto> AwaitingReply => Invitations.Where(i => !i.IsSelected);
    }
}
