using System.Collections.Generic;

namespace ResearchPublicationManagementSystem.Models
{
    public class CategoryListViewModel
    {
        public ListToolbarViewModel Toolbar { get; set; } = new();

        public List<CategoryListItemViewModel> Categories { get; set; } = new();
    }
}