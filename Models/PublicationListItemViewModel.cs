namespace ResearchPublicationManagementSystem.Models
{
    public class PublicationListItemViewModel
    {
        public int Id { get; set; }

        public string PublicationId { get; set; } = "";

        public string Title { get; set; } = "";

        public string Student { get; set; } = "";

        public string Supervisor { get; set; } = "";

        public List<string> CommitteeMembers { get; set; } = new();

        public string ResearchArea { get; set; } = "";

        public string Status { get; set; } = "";

        public string SubmittedDate { get; set; } = "";

        public string LastUpdated { get; set; } = "";
    }
}