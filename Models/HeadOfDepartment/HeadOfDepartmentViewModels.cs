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
    public class HeadOfDepartmentEthicsViewModel
    {
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
        public List<DepartmentProposalItem> Items { get; set; } = [];

        public bool LoadFailed { get; set; }
    }

    public class DepartmentProposalItem
    {
        public PublicationContainerDto Container { get; set; } = null!;

        public IReadOnlyList<ProposalDto> Proposals { get; set; } = [];
    }
}
