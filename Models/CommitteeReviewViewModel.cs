namespace ResearchPublicationManagementSystem.Models
{
    public class CommitteeReviewViewModel
    {
        public int Id { get; set; }

        public string ReviewerName { get; set; } = "";

        public string CommitteeRole { get; set; } = "";

        public DateTime ReviewDate { get; set; }

        public string Recommendation { get; set; } = "";

        public string Comments { get; set; } = "";
    }
}