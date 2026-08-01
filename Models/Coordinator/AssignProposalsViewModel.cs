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

        /// <summary>The supervisors on this page of the chooser, and the pager under it.</summary>
        public IReadOnlyList<UserListItemDto> Supervisors { get; set; } = [];

        /// <summary>How many are available altogether, which is what "all" means on the button.</summary>
        public int SupervisorsTotal { get; set; }

        /// <summary>
        /// The coordinator's own saved sets of supervisors. Ticking a group's members is the same
        /// as ticking them one at a time: a group fills in the form and grants nothing.
        /// </summary>
        public IReadOnlyList<SupervisorGroupDto> Groups { get; set; } = [];

        /// <summary>
        /// How many students, and how many proposals of theirs, are in this queue for a second time
        /// after a round that found nobody willing. Counted over the whole queue rather than this
        /// page: it is the figure that decides whether to send another batch or ask those students
        /// for new work, and a page is not the queue.
        /// </summary>
        public int ReturnedStudents { get; set; }
        public int ReturnedProposals { get; set; }

        public bool HasReturned => ReturnedProposals > 0;

        /// <summary>
        /// What was typed to narrow the chooser. The narrowing is the API's, because it knows who
        /// is available; the paging under it is the browser's, because a page turn must not lose
        /// a tick made on the page being left.
        /// </summary>
        public string? SupervisorSearch { get; set; }

        /// <summary>
        /// The order and the search over the proposals themselves, both applied by the API. The
        /// default is oldest first, and it stays that way rather than being left to the endpoint's
        /// own preference: a dispatch queue is worked from the front, and the proposal a student
        /// has been waiting longest on is the one that should be nearest the top.
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
            if (!string.IsNullOrWhiteSpace(SupervisorSearch)) values["supervisorSearch"] = SupervisorSearch;
            return values;
        }

        /// <summary>
        /// Where Clear goes: this screen again, without the search term and with everything else
        /// exactly as it is. Dropping the ordering as well would answer a question nobody asked,
        /// and the reader would have to set it again to get back to the list they were reading.
        /// </summary>
        public Dictionary<string, string?> ClearSearchRoute() =>
            RouteValues().Where(v => v.Key != "search").ToDictionary(v => v.Key, v => v.Value);

        public SortBarViewModel SortBar => new()
        {
            Controller = "Coordinator",
            Action = "assigning_proposal_forsupervisor",
            Sort = Sort,
            Descending = Descending,
            RouteValues = RouteValues()
                .Where(v => v.Key != "sort" && v.Key != "desc")
                .ToDictionary(v => v.Key, v => v.Value),
            Columns =
            [
                ("submitted", "Date", true),
                ("student", "Student", false),
                ("title", "Proposal", false)
            ]
        };

        public bool HasSupervisorSearch => !string.IsNullOrWhiteSpace(SupervisorSearch);

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

        /// <summary><inheritdoc cref="AssignProposalsViewModel.ClearSearchRoute" path="/summary"/></summary>
        public Dictionary<string, string?> ClearSearchRoute() =>
            RouteValues().Where(v => v.Key != "search").ToDictionary(v => v.Key, v => v.Value);

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
