namespace ResearchPublicationManagementSystem.Models
{
    public class ListToolbarViewModel
    {
        public string SearchPlaceholder { get; set; } = "";

        public string SearchText { get; set; } = "";

        public bool ShowButton { get; set; } = true;

        public string ButtonText { get; set; } = "";

        public string Controller { get; set; } = "";

        public string Action { get; set; } = "";

        public bool ShowStatusFilter { get; set; }

        public bool ShowCategoryFilter { get; set; }

        public bool ShowSupervisorFilter { get; set; }
    }
}
