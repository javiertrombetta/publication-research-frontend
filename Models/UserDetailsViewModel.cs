namespace ResearchPublicationManagementSystem.Models
{
    public class UserDetailsViewModel
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Department { get; set; } = "";

        public string Role { get; set; } = "";
        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime LastLogin { get; set; }

        // Role Information
        public string Supervisor { get; set; } = "";
        public string CommitteeMember1 { get; set; } = "";
        public string CommitteeMember2 { get; set; } = "";
        public string ResearchCoordinator { get; set; } = "";

        // Publication Information
        public string CurrentPublication { get; set; } = "";
        public string PublicationStatus { get; set; } = "";
    }
}
