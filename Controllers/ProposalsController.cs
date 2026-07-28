using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ResearchPublicationManagementSystem.Models;

namespace ResearchPublicationManagementSystem.Controllers
{
    public class ProposalsController : Controller
    {
        public IActionResult Index()
        {
            var proposals = new List<ProposalListItemViewModel>
            {
                new()
                {
                    Id = 1,
                    ProposalId = "PRP-001",
                    Title = "AI-Based Research Publication Management System",
                    Student = "John Smith",
                    Category = "Computer Science",
                    Supervisor = "Dr. Brown",
                    Status = "Pending Assignment",
                    SubmittedDate = "20 Jul 2026",
                    LastUpdated = "20 Jul 2026"
                },

                new()
                {
                    Id = 2,
                    ProposalId = "PRP-002",
                    Title = "Machine Learning for Healthcare",
                    Student = "Jane Wilson",
                    Category = "Artificial Intelligence",
                    Supervisor = "Dr. Wilson",
                    Status = "Under Review",
                    SubmittedDate = "18 Jul 2026",
                    LastUpdated = "21 Jul 2026"
                },

                new()
                {
                    Id = 3,
                    ProposalId = "PRP-003",
                    Title = "Cloud Security Framework",
                    Student = "Michael Lee",
                    Category = "Cyber Security",
                    Supervisor = "Dr. Johnson",
                    Status = "Approved",
                    SubmittedDate = "10 Jul 2026",
                    LastUpdated = "19 Jul 2026"
                }
            };

            var model = new ProposalListViewModel
            {
                Toolbar = new SearchFilterToolbarViewModel
                {
                    SearchPlaceholder = "Search proposals...",
                    PersonLabel = "Supervisor",

                    StatusOptions = new List<SelectListItem>
    {
        new SelectListItem { Text = "All Statuses", Value = "" },
        new SelectListItem { Text = "Pending Assignment", Value = "Pending Assignment" },
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
        new SelectListItem { Text = "All Supervisors", Value = "" },
        new SelectListItem { Text = "Dr. Brown", Value = "Dr. Brown" },
        new SelectListItem { Text = "Dr. Wilson", Value = "Dr. Wilson" },
        new SelectListItem { Text = "Dr. Johnson", Value = "Dr. Johnson" }
    }
                },

                TotalProposals = 32,
                PendingAssignment = 8,
                UnderReview = 15,
                Approved = 9,

                Proposals = proposals
            };

            return View(model);
        }
    }
}