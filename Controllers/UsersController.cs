using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Models;

namespace ResearchPublicationManagementSystem.Controllers
{
    public class UsersController : Controller
    {
        public IActionResult Index()
        {
            List<UserListItemViewModel> users = new()
    {
        new UserListItemViewModel
        {
            FullName = "John Doe",
            Email = "john.doe@example.com",
            Role = "Administrator",
            IsActive = true,
            CreatedDate = new DateTime(2026,1,15)
        },

        new UserListItemViewModel
        {
            FullName = "Sarah Johnson",
            Email = "sarah.johnson@example.com",
            Role = "Student",
            IsActive = true,
            CreatedDate = new DateTime(2026,2,20)
        },

        new UserListItemViewModel
        {
            FullName = "Michael Brown",
            Email = "michael.brown@example.com",
            Role = "Supervisor",
            IsActive = true,
            CreatedDate = new DateTime(2026,1,10)
        },

        new UserListItemViewModel
        {
            FullName = "David Wilson",
            Email = "david.wilson@example.com",
            Role = "Coordinator",
            IsActive = false,
            CreatedDate = new DateTime(2026,2,12)
        }
    };

            UserListViewModel model = new UserListViewModel
            {
                Toolbar = new ListToolbarViewModel
                {
                    SearchPlaceholder = "Search users...",
                    ButtonText = "Add User",
                    Controller = "Users",
                    Action = "Create"
                },

                Users = users
            };

            return View(model);
        }
      /**  [HttpPost] **/
        public IActionResult Details()
        {
            UserDetailsViewModel model = new()
            {
                FullName = "John Doe",
                Email = "john.doe@example.com",
                Phone = "+64 xxxxxxxx",
                Department = "Computer Science",

                Role = "Student",
                IsActive = true,

                CreatedDate = new DateTime(2026, 1, 15),
                LastLogin = new DateTime(2026, 4, 7, 10, 30, 0),

                Supervisor = "Dr Sarah Chen",
                CommitteeMember1 = "Prof John Smith",
                CommitteeMember2 = "Dr Michael Tan",
                ResearchCoordinator = "Mary Lee",

                CurrentPublication = "AI-Driven Research Publication Management System",
                PublicationStatus = "Under Review"
            };

            return View(model);
        }
        public IActionResult Create()
        {
            CreateUserViewModel model = new();

            return View(model);
        }
        public IActionResult Edit()
        {
            var model = new EditUserViewModel
            {
                Id = "1",
                FullName = "John Smith",
                Email = "john.smith@test.com",
                Phone = "0211234567",
                Department = "Computer Science",
                Role = "Student",
                IsActive = true
            };

            return View(model);
        }

    }

}
