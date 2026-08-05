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
        /// How many students are in this queue for a second time after a round no supervisor was
        /// interested in, and how many proposals of theirs that comes to. Counted over the whole
        /// queue rather than this page: it is the figure that decides whether to send another batch
        /// or ask those students for new work, and a page is not the queue.
        ///
        /// The student is the figure the screen states. A student only comes back when nothing of
        /// theirs interested anybody, so the proposal count is the number they happened to write
        /// rather than anything about the round.
        /// </summary>
        public int ReturnedStudents { get; set; }
        public int ReturnedProposals { get; set; }

        public bool HasReturned => ReturnedStudents > 0;

        /// <summary>
        /// What the answer-by field starts on: now plus the institution's expected supervisor
        /// response time, in the reader's own time. The date is required, and the institution has
        /// already decided how long it expects a supervisor to take, so making a coordinator work
        /// it out on every send would be asking them to restate a policy that already exists.
        /// </summary>
        public DateTime SuggestedRespondBy { get; set; } = DateTime.Now.AddDays(14);

        /// <summary>Formatted for a datetime-local input, which takes no offset or seconds.</summary>
        public string SuggestedRespondByValue => SuggestedRespondBy.ToString("yyyy-MM-ddTHH:mm");

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

        /// <summary>
        /// One sortable heading for the row that stands above these groups. The rest of the
        /// screen's state travels with it and the ordering does not: the heading is what sets the
        /// ordering, so carrying the current one would fight with the click.
        /// </summary>
        public SortableColumnViewModel Column(string column, string label, bool descendingFirst = false) => new()
        {
            Controller = "Coordinator",
            Action = "assigning_proposal_forsupervisor",
            Column = column,
            Label = label,
            CurrentSort = Sort,
            CurrentDescending = Descending,
            DescendingFirst = descendingFirst,
            RouteValues = RouteValues()
                .Where(v => v.Key != "sort" && v.Key != "desc")
                .ToDictionary(v => v.Key, v => v.Value)
        };

        public bool HasSupervisorSearch => !string.IsNullOrWhiteSpace(SupervisorSearch);

        /// <summary>
        /// Where "show every available supervisor" goes: this screen again with the chooser
        /// widened, and everything the reader chose about the proposals left alone.
        /// </summary>
        public Dictionary<string, string?> ClearSupervisorSearchRoute() =>
            RouteValues().Where(v => v.Key != "supervisorSearch").ToDictionary(v => v.Key, v => v.Value);

        public bool LoadFailed { get; set; }

        /// <summary>
        /// The student a group belongs to, and enough to know which student that is.
        ///
        /// A name is not an identifier. The same person appears once per publication they have
        /// open, so a coordinator can be looking at two groups headed identically, and two people
        /// can share a name outright. The id and the address settle both.
        /// </summary>
        public ProposalWithInvitationsDto? FirstIn(Guid containerId) =>
            Proposals.FirstOrDefault(p => p.PublicationContainerId == containerId);

        public string StudentFor(Guid containerId) =>
            FirstIn(containerId)?.StudentName ?? "Unknown student";

        /// <summary>
        /// When this student sent the round in. One date for the group, because a student submits
        /// their proposals together: it is the round that was sent, not each proposal separately.
        /// The earliest, so an older proposal added to a round still dates the round from its
        /// start rather than from the last thing touched.
        /// </summary>
        public DateTime? SubmittedFor(Guid containerId) =>
            Proposals
                .Where(p => p.PublicationContainerId == containerId && p.SubmittedAt is not null)
                .Min(p => p.SubmittedAt);

        /// <summary>Proposals grouped by student, since they are sent out per student.</summary>
        public IEnumerable<IGrouping<Guid, ProposalWithInvitationsDto>> ByPublication =>
            Proposals.GroupBy(p => p.PublicationContainerId);

        // ---------- What the coordinator had chosen when a send was refused ----------

        /// <summary>
        /// Which page of the queue this is, carried so that a refused send comes back to the page
        /// the coordinator was working on rather than to the first one.
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Set only when a send has just been refused. Null on an ordinary visit, which is what
        /// tells the screen to fall back to its defaults rather than to an empty selection.
        ///
        /// A refusal has to leave the screen exactly as it was. Rebuilding a batch of ticks
        /// across a queue and a supervisor chooser is most of the work of the screen, and losing
        /// it over a missing sentence is the difference between a correction and starting again.
        /// </summary>
        public IReadOnlyList<Guid>? ChosenProposalIds { get; set; }
        public IReadOnlyList<Guid>? ChosenSupervisorIds { get; set; }

        /// <summary>What was typed in the message, kept for the same reason.</summary>
        public string? Comments { get; set; }

        /// <summary>The answer-by date as it was filled in, or the suggestion on a first visit.</summary>
        public DateTime? ChosenRespondBy { get; set; }

        public string RespondByValue =>
            (ChosenRespondBy ?? SuggestedRespondBy).ToString("yyyy-MM-ddTHH:mm");

        /// <summary>
        /// Every proposal on the page is ticked to begin with, because sending the whole queue is
        /// the ordinary case. After a refusal it is what the coordinator had ticked, including
        /// none of them in a group they had cleared.
        /// </summary>
        public bool IsProposalChosen(Guid proposalId) =>
            ChosenProposalIds is null || ChosenProposalIds.Contains(proposalId);

        /// <summary>Nobody to begin with: who to send to is the coordinator's whole decision here.</summary>
        public bool IsSupervisorChosen(Guid supervisorId) =>
            ChosenSupervisorIds is not null && ChosenSupervisorIds.Contains(supervisorId);
    }

    /// <summary>Proposals a supervisor has offered to take on, awaiting the coordinator.</summary>
    public class SupervisorSelectionsViewModel
    {
        /// <summary>
        /// Whether supervisors are asked which proposals they will take on before the coordinator
        /// appoints one. It decides what this screen offers: a choice between the people who
        /// offered, or a choice between everybody who could.
        /// </summary>
        public bool SupervisorsExpressInterest { get; set; } = true;

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
        /// <summary>
        /// Everybody who could take this on, for the institutions that do not ask supervisors
        /// first. Empty where they do: the choice there is between the people who offered.
        /// </summary>
        public IReadOnlyList<UserListItemDto> Available { get; set; } = [];

        /// <summary>Carried on the proposal itself, so the screen needs no second request to name the author.</summary>
        public string StudentName { get; set; } = string.Empty;

        public ProposalDto Proposal { get; set; } = null!;

        public IReadOnlyList<SupervisorInvitationDto> Invitations { get; set; } = [];

        /// <summary>The supervisors who said yes, the only ones who can actually be assigned.</summary>
        public IEnumerable<SupervisorInvitationDto> Willing => Invitations.Where(i => i.IsSelected);

        public IEnumerable<SupervisorInvitationDto> AwaitingReply => Invitations.Where(i => !i.IsSelected);

        /// <summary>
        /// When this round runs out, in the reader's own time, and null where the coordinator set
        /// no date. Read off the invitations because that is where it lives: every invitation in
        /// one send carries the same date, so the earliest is the round's.
        /// </summary>
        public DateTime? RespondBy => Invitations
            .Where(i => i.RespondBy is not null)
            .Select(i => i.RespondBy!.Value.ToLocalTime())
            .DefaultIfEmpty()
            .Min() is var earliest && earliest == default ? null : earliest;

        public bool RespondByHasPassed => RespondBy is { } by && by < DateTime.Now;
    }
}
