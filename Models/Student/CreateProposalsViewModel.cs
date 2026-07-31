namespace ResearchPublicationManagementSystem.Models
{
    public class CreateProposalsViewModel
    {
        public Guid ContainerId { get; set; }

        public bool IsLocked { get; set; }

        public List<ProposalSlotViewModel> Slots { get; set; } =
        [
            new(), new(), new()
        ];
    }

    public class ProposalSlotViewModel
    {
        public Guid? ProposalId { get; set; }

        public string? Title { get; set; }

        public string? Abstract { get; set; }

        public string? Status { get; set; }
    }
}
