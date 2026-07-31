namespace ResearchPublicationManagementSystem.Models
{
    public class CommitteeMemberViewModel
    {
        public int Id { get; set; }

        public string MemberName { get; set; } = "";

        public string CommitteeRole { get; set; } = "";

        public string ReviewStatus { get; set; } = "";

        public string Recommendation { get; set; } = "";
    }
}