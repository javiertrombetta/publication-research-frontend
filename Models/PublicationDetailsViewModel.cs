namespace ResearchPublicationManagementSystem.Models
{
    public class PublicationDetailsViewModel
    {
        // ===== Publication =====

        public int Id { get; set; }

        public string PublicationId { get; set; } = "";

        public string Title { get; set; } = "";

        public string Status { get; set; } = "";

        public string Abstract { get; set; } = "";

        public string Version { get; set; } = "";

        public DateTime SubmittedDate { get; set; }

        public DateTime LastUpdated { get; set; }

        // ===== Related Proposal =====

        public string RelatedProposalId { get; set; } = "";

        // ===== Student =====

        public string StudentId { get; set; } = "";

        public string StudentName { get; set; } = "";

        public string StudentEmail { get; set; } = "";

        // ===== Supervisor =====

        public string SupervisorName { get; set; } = "";

        // ===== Research =====

        public string ResearchCategory { get; set; } = "";

        // ===== Committee =====

        public string CommitteeName { get; set; } = "";

        // ===== Collections =====

        public List<CommitteeMemberViewModel> CommitteeMembers { get; set; } = new();

        public List<CommitteeReviewViewModel> CommitteeReviews { get; set; } = new();

        public List<PublicationWorkflowItemViewModel> Workflow { get; set; } = new();

        public List<PublicationHistoryItemViewModel> History { get; set; } = new();

        public List<ActivityLogItemViewModel> ActivityLogs { get; set; } = new();
    }
}