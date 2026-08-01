using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// The coordinator's workload: the publications they oversee, and what each one is waiting on.
    /// Built from two calls rather than one per publication. The container listing already carries
    /// the stage, the ethics status and the paper's status.
    /// </summary>
    public class CoordinatorDashboardViewModel
    {
        public IReadOnlyList<PublicationContainerDto> Publications { get; set; } = [];

        /// <summary>
        /// Submitted proposals no supervisor has been invited to yet. These are the coordinator's
        /// most immediate task: nothing else in the pipeline moves until they go out.
        /// </summary>
        public IReadOnlyList<ProposalWithInvitationsDto> ProposalsAwaitingDispatch { get; set; } = [];

        /// <summary>
        /// How many are still moving, not how many exist and not how many are on this page. The
        /// dashboard states figures, and a figure capped at the page size would simply be wrong.
        /// </summary>
        public int PublicationsTotal { get; set; }
        public int ProposalsAwaitingDispatchTotal { get; set; }

        /// <summary>
        /// Proposals a supervisor has offered to take on and nobody has been assigned to yet. The
        /// size of the Supervisor selections queue, so the card that points at that screen says how
        /// much is behind it.
        /// </summary>
        public int SupervisorRepliesTotal { get; set; }

        /// <summary>
        /// The coordinator's two ethics queues, by size. Both are stated because they are two
        /// screens: a single "ethics" figure would tell somebody there was work without saying
        /// which of the two decisions it was waiting on.
        /// </summary>
        public int EthicsDecisionsTotal { get; set; }
        public int FinalEthicsDecisionsTotal { get; set; }

        public bool LoadFailed { get; set; }

        /// <summary>
        /// Which page of the publications table this is, and how it is ordered. Both applied by
        /// the API: a coordinator with two hundred publications was being handed all of them, and
        /// ordering the ten on screen would not have been ordering the table.
        /// </summary>
        public PagerViewModel? Pager { get; set; }
        public string? Sort { get; set; }
        public bool Descending { get; set; }

        public Dictionary<string, string?> RouteValues()
        {
            var values = new Dictionary<string, string?>();
            if (!string.IsNullOrWhiteSpace(Sort)) values["sort"] = Sort;
            if (Descending) values["desc"] = "true";
            return values;
        }

        /// <summary>
        /// One clickable heading. Built per column rather than through the shared bar, because
        /// this listing is a real table and its headings are where a reader expects to click.
        /// </summary>
        public SortableColumnViewModel Column(string column, string label, bool descendingFirst = false) => new()
        {
            Controller = "Coordinator",
            Action = "Coordinator_dashboard",
            Column = column,
            Label = label,
            CurrentSort = Sort,
            CurrentDescending = Descending,
            DescendingFirst = descendingFirst
        };

        public int ActionsWaiting =>
            (ProposalsAwaitingDispatchTotal > 0 ? 1 : 0) +
            Publications.Count(p => ActionFor(p) is not null);

        /// <summary>
        /// What the coordinator has to do next for a publication, or null when the ball is in
        /// someone else's court. Derived from the stage and the two statuses the listing carries,
        /// so a coordinator can see their queue without opening every publication.
        /// </summary>
        public static CoordinatorAction? ActionFor(PublicationContainerDto publication)
        {
            if (publication.Status == "Completed") return null;

            // Which ethics step it is comes from the backend's own reading of the workflow, and it
            // has to be the step rather than the role: the coordinator has two turns in ethics and
            // they are two different screens. Sending both to the first one meant a publication
            // waiting on the final decision offered a button that landed on an empty queue.
            if (publication.EthicsAwaitingStep is { } step
                && step is EthicsSteps.CoordinatorConfirmation
                        or EthicsSteps.CoordinatorDocumentReview
                        or EthicsSteps.CoordinatorFinalDecision)
            {
                return new CoordinatorAction("Ethics decision",
                    step == EthicsSteps.CoordinatorFinalDecision
                        ? "Ethic_review_afters_headofdepartment"
                        : "Ethic_review_aftersupervisor",
                    SearchTermFor(publication));
            }

            // The paper's own wait, not its status. UnderReview covers four of them: the supervisor
            // reading it, an admin appointing a committee, the committee voting, and only then the
            // coordinator. Offering the decision on all four was offering work that was not theirs
            // yet, and the screen that carries it out lists none of those first three.
            if (publication.PaperAwaitingRole == RoleNames.Coordinator)
            {
                return new CoordinatorAction("Decide on the research paper", "Evaluation_after_committee",
                    SearchTermFor(publication));
            }

            return null;
        }

        /// <summary>
        /// What to put in the target screen's search box so the row the coordinator just clicked is
        /// the one in front of them when they arrive.
        ///
        /// The title, when there is one. A publication with no title yet displays as "Untitled
        /// publication", which is a label this screen writes rather than anything the search could
        /// match, so searching for it would land somebody on an empty list. The student's name is
        /// the next best thing: always present, and it narrows the queue to their work.
        /// </summary>
        private static string SearchTermFor(PublicationContainerDto publication) =>
            string.IsNullOrWhiteSpace(publication.Title) ? publication.StudentName : publication.Title!;
    }

    /// <summary>
    /// A task waiting on the coordinator, the screen that carries it out, and the term that screen
    /// should arrive already searching for.
    /// </summary>
    public record CoordinatorAction(string Label, string Action, string Search);
}
