using System.Collections.Generic;

namespace ResearchPublicationManagementSystem.Models
{
    public class UserListViewModel
    {
        public ListToolbarViewModel Toolbar { get; set; } = new();

        public List<UserListItemViewModel> Users { get; set; } = new();
    }
}
