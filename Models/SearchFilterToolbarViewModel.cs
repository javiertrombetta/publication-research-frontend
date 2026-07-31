using Microsoft.AspNetCore.Mvc.Rendering;

namespace ResearchPublicationManagementSystem.Models
{
    public class SearchFilterToolbarViewModel
    {
        // Search
        public string SearchPlaceholder { get; set; } = "";
        public string SearchText { get; set; } = "";

        // Status
        public string SelectedStatus { get; set; } = "";
        public List<SelectListItem> StatusOptions { get; set; } = new();

        // Category
        public string SelectedCategory { get; set; } = "";
        public List<SelectListItem> CategoryOptions { get; set; } = new();

        // Supervisor / Committee
        public string SelectedPerson { get; set; } = "";
        public string PersonLabel { get; set; } = "";
        public List<SelectListItem> PersonOptions { get; set; } = new();

        // Optional button
        public bool ShowButton { get; set; }

        public string ButtonText { get; set; } = "";

        public string Controller { get; set; } = "";

        public string Action { get; set; } = "";
    }
}
