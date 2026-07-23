using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Models;

namespace ResearchPublicationManagementSystem.Controllers
{
    public class CategoriesController : Controller
    {
        public IActionResult Index()
        {
            var model = new CategoryListViewModel
            {
                Toolbar = new ListToolbarViewModel
                {
                    SearchPlaceholder = "Search categories...",
                    ButtonText = "Add Category",
                    Controller = "Categories",
                    Action = "Create"
                },

                Categories = new List<CategoryListItemViewModel>
                {
                    new CategoryListItemViewModel
                    {
                        Id = 1,
                        CategoryName = "Artificial Intelligence",
                        Description = "AI related research",
                        PublicationCount = 12,
                        IsActive = true
                    },

                    new CategoryListItemViewModel
                    {
                        Id = 2,
                        CategoryName = "Software Engineering",
                        Description = "Software design and development",
                        PublicationCount = 8,
                        IsActive = true
                    },

                    new CategoryListItemViewModel
                    {
                        Id = 3,
                        CategoryName = "Business Analytics",
                        Description = "Business intelligence and analytics",
                        PublicationCount = 4,
                        IsActive = false
                    }
                }
            };

            return View(model);
        }

        public IActionResult Create()
        {
            var model = new CategoryViewModel
            {
                CategoryName = "",
                Description = "",
                IsActive = true
            };

            return View(model);
        }

        public IActionResult Edit()
        {
            var model = new CategoryViewModel
            {
                Id = 1,
                CategoryName = "Artificial Intelligence",
                Description = "AI related research",
                IsActive = true
            };

            return View(model);
        }
    }
}