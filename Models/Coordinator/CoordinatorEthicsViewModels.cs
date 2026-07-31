using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// The publications waiting on the coordinator at one point of the ethics workflow. Both of
    /// the coordinator's ethics screens use this — what differs is which decision each offers.
    /// </summary>
    public class CoordinatorEthicsViewModel
    {
        public List<CoordinatorEthicsItem> Items { get; set; } = [];

        /// <summary>Which of the coordinator's two ethics steps this screen is showing.</summary>
        public string Stage { get; set; } = string.Empty;

        public bool LoadFailed { get; set; }
    }

    public class CoordinatorEthicsItem
    {
        public PublicationContainerDto Container { get; set; } = null!;

        public EthicsApprovalDto Approval { get; set; } = null!;

        public IReadOnlyList<EthicsDocumentDto> Documents { get; set; } = [];

        /// <summary>
        /// True when a supervisor found no documentation necessary and the coordinator is being
        /// asked to confirm or override that, rather than to review uploaded documents.
        /// </summary>
        public bool IsNotRequiredConfirmation =>
            Approval.Status == EthicsStatus.NotRequired;
    }

    /// <summary>Research papers awaiting the coordinator's final decision.</summary>
    public class CoordinatorPapersViewModel
    {
        public List<CoordinatorPaperItem> Items { get; set; } = [];

        public bool LoadFailed { get; set; }
    }

    public class CoordinatorPaperItem
    {
        public PublicationContainerDto Container { get; set; } = null!;

        public PublicationDto Paper { get; set; } = null!;

        /// <summary>Every review recorded so far — the supervisor's and the committee's.</summary>
        public IReadOnlyList<ReviewDto> Reviews { get; set; } = [];
    }
}
