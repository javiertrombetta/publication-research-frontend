using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ResearchPublicationManagementSystem.Models;

namespace ResearchPublicationManagementSystem.Controllers
{
    public class PublicationsController : Controller
    {
        public IActionResult Index()
        {
            var publications = new List<PublicationListItemViewModel>
{
    new()
    {
        Id = 1,
        PublicationId = "PUB-001",
        Title = "AI-Based Research Publication Management System",
        Student = "John Smith",
        Supervisor = "Dr. Brown",
        CommitteeMembers = new List<string>
        {
            "Dr. Brown",
            "Prof. David Tan",
            "Dr. Emily Wong"
        },
        ResearchArea = "Computer Science",
        Status = "Under Review",
        SubmittedDate = "22 Jul 2026",
        LastUpdated = "23 Jul 2026"
    },

    new()
    {
        Id = 2,
        PublicationId = "PUB-002",
        Title = "Machine Learning for Healthcare",
        Student = "Jane Wilson",
        Supervisor = "Dr. Wilson",
        CommitteeMembers = new List<string>
        {
            "Dr. Wilson",
            "Dr. Michael Lim"
        },
        ResearchArea = "Artificial Intelligence",
        Status = "Approved",
        SubmittedDate = "18 Jul 2026",
        LastUpdated = "21 Jul 2026"
    },

    new()
    {
        Id = 3,
        PublicationId = "PUB-003",
        Title = "Cloud Security Framework",
        Student = "Michael Lee",
        Supervisor = "Dr. Johnson",
        CommitteeMembers = new(),
        ResearchArea = "Cyber Security",
        Status = "Pending Committee Assignment",
        SubmittedDate = "15 Jul 2026",
        LastUpdated = "20 Jul 2026"
    }
};

            var model = new PublicationListViewModel
            {
                Toolbar = new SearchFilterToolbarViewModel
                {
                    SearchPlaceholder = "Search publications...",
                    PersonLabel = "Committee",

                    StatusOptions = new List<SelectListItem>
                    {
                        new SelectListItem { Text = "All Statuses", Value = "" },
                        new SelectListItem { Text = "Pending Committee Assignment", Value = "Pending Committee Assignment" },
                        new SelectListItem { Text = "Under Review", Value = "Under Review" },
                        new SelectListItem { Text = "Approved", Value = "Approved" }
                    },

                    CategoryOptions = new List<SelectListItem>
                    {
                        new SelectListItem { Text = "All Categories", Value = "" },
                        new SelectListItem { Text = "Computer Science", Value = "Computer Science" },
                        new SelectListItem { Text = "Artificial Intelligence", Value = "Artificial Intelligence" },
                        new SelectListItem { Text = "Cyber Security", Value = "Cyber Security" }
                    },

                    PersonOptions = new List<SelectListItem>
                    {
                        new SelectListItem { Text = "All Committees", Value = "" },
                        new SelectListItem { Text = "Committee A", Value = "Committee A" },
                        new SelectListItem { Text = "Committee B", Value = "Committee B" },
                        new SelectListItem { Text = "Committee C", Value = "Committee C" }
                    }
                },

                TotalPublications = 86,
                PendingCommitteeAssignment = 37,
                UnderReview = 5,
                Approved = 44,

                Publications = publications
            };

            return View(model);
        }
    }
}
