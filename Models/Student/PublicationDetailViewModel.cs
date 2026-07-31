using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>Everything belonging to ONE publication: its proposals, ethics workflow and paper.</summary>
    public class PublicationDetailViewModel
    {
        public PublicationContainerDto Container { get; set; } = null!;

        public IReadOnlyList<ProposalDto> Proposals { get; set; } = [];

        public EthicsApprovalDto? EthicsApproval { get; set; }

        public PublicationDto? Publication { get; set; }

        /// <summary>
        /// Every recorded action on this publication, newest first, with the comment each actor
        /// left. Shown as its own tab so the student can follow the whole chronology.
        /// </summary>
        public IReadOnlyList<ActivityHistoryEntryDto> History { get; set; } = [];

        public string DisplayTitle =>
            string.IsNullOrWhiteSpace(Container.Title) ? "Untitled publication" : Container.Title!;
    }
}
