namespace ResearchPublicationManagementSystem.Models
{
    public class CreateProposalsViewModel
    {
        public Guid ContainerId { get; set; }

        public bool IsLocked { get; set; }

        /// <summary>
        /// How many proposals this round takes, as the institution has it. The screen used to
        /// offer three because three was written into it; an administrator now says, and the
        /// same figures govern a round a coordinator has asked for again.
        /// </summary>
        public int Fewest { get; set; } = 1;

        public int Most { get; set; } = 3;

        public List<ProposalSlotViewModel> Slots { get; set; } = [];

        /// <summary>What the screen says it is asking for, before anybody fills anything in.</summary>
        public string RoundSize => Fewest == Most
            ? $"This round takes {Fewest} research {(Fewest == 1 ? "proposal" : "proposals")}."
            : $"This round takes between {Fewest} and {Most} research proposals.";
    }

    public class ProposalSlotViewModel
    {
        public Guid? ProposalId { get; set; }

        public string? Title { get; set; }

        public string? Abstract { get; set; }

        public string? Status { get; set; }
    }
}
