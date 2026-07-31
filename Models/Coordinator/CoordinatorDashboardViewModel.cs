using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// The coordinator's workload: the publications they oversee, and what each one is waiting on.
    /// Built from two calls rather than one per publication — the container listing already
    /// carries the stage, the ethics status and the paper's status.
    /// </summary>
    public class CoordinatorDashboardViewModel
    {
        public IReadOnlyList<PublicationContainerDto> Publications { get; set; } = [];

        /// <summary>
        /// Submitted proposals no supervisor has been invited to yet. These are the coordinator's
        /// most immediate task: nothing else in the pipeline moves until they go out.
        /// </summary>
        public IReadOnlyList<ProposalDto> ProposalsAwaitingDispatch { get; set; } = [];

        /// <summary>
        /// How many there are in total, not how many are on this page. The dashboard states
        /// figures, and a figure capped at the page size would simply be wrong.
        /// </summary>
        public int PublicationsTotal { get; set; }
        public int ProposalsAwaitingDispatchTotal { get; set; }

        public bool LoadFailed { get; set; }

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

            // Which ethics step it is comes from the backend's own reading of the workflow —
            // EthicsStatus alone can't tell the coordinator's two turns apart.
            if (publication.EthicsAwaitingRole == RoleNames.Coordinator)
            {
                return new CoordinatorAction("Ethics decision", "Ethic_review_aftersupervisor");
            }

            if (publication.PaperStatus == PublicationStatus.UnderReview)
            {
                return new CoordinatorAction("Decide on the research paper", "Evaluation_after_committee");
            }

            return null;
        }
    }

    /// <summary>A task waiting on the coordinator, and the screen that carries it out.</summary>
    public record CoordinatorAction(string Label, string Action);
}
