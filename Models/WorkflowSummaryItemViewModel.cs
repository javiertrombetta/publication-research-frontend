namespace ResearchPublicationManagementSystem.Models
{
    public class WorkflowSummaryItemViewModel
    {
        public string Title { get; set; } = "";

        public int Current { get; set; }

        public int Total { get; set; }

        public string BadgeText { get; set; } = "";

        public string ProgressColor { get; set; } = "bg-primary";

        public int Percentage =>
            Total == 0 ? 0 : (Current * 100 / Total);
    }
}
