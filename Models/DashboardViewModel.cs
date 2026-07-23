
namespace ResearchPublicationManagementSystem.Models
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }

        public int PendingWorkflow { get; set; }

        public int Publications { get; set; }

        public int OnlineUsers { get; set; }
        public List<WorkflowSummaryItemViewModel> WorkflowSummary { get; set; } = new();
    }
}
