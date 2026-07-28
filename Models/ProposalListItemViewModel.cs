namespace ResearchPublicationManagementSystem.Models
{
    public class ProposalListItemViewModel
    {
        public int Id { get; set; }

        public string ProposalId { get; set; } = "";

        public string Title { get; set; } = "";

        public string Student { get; set; } = "";

        public string Category { get; set; } = "";

        public string Supervisor { get; set; } = "";

        public string Status { get; set; } = "";

        public string SubmittedDate { get; set; } = "";

        public string LastUpdated { get; set; } = "";
    }
}
