namespace ResearchPublicationManagementSystem.Models
{
    public class PublicationWorkflowItemViewModel
    {
        public int Order { get; set; }

        public string StepName { get; set; } = "";

        public bool IsCompleted { get; set; }

        public bool IsCurrentStep { get; set; }
    }
}
