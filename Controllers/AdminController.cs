using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Models;
namespace ResearchPublicationManagementSystem.Controllers
{    

    public class AdminController : Controller
    {


        public IActionResult Dashboard()
        {
            DashboardViewModel model = new DashboardViewModel
            {
                TotalUsers = 342,
                PendingWorkflow = 35,
                Publications = 120,
                OnlineUsers = 12,

                WorkflowSummary = new List<WorkflowSummaryItemViewModel>
        {
            new WorkflowSummaryItemViewModel
            {
                Title = "Proposal Assignment",
                Current = 12,
                Total = 40,
                BadgeText = "12 Pending",
                ProgressColor = "bg-danger"
            },

            new WorkflowSummaryItemViewModel
            {
                Title = "Ethics Approval",
                Current = 8,
                Total = 30,
                BadgeText = "8 Pending",
                ProgressColor = "bg-warning"
            },

            new WorkflowSummaryItemViewModel
            {
                Title = "Publication Approval",
                Current = 15,
                Total = 50,
                BadgeText = "15 Pending",
                ProgressColor = "bg-primary"
            }
        }
            };

            return View(model);
        }

    }
}
