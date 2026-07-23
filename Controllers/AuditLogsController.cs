using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Models;

namespace ResearchPublicationManagementSystem.Controllers
{
    public class AuditLogsController : Controller
    {
        public IActionResult Index()
        {
            var model = new AuditLogViewModel
            {
                TotalLogs = 2485,
                TodayLogs = 42,
                ActiveUsers = 87,
                CriticalEvents = 3
            };

            model.AuditLogs.AddRange(new List<AuditLogItemViewModel>
            {
                new AuditLogItemViewModel
                {
                    DateTime = DateTime.Now.AddMinutes(-10),
                    User = "admin@test.com",
                    Module = "Users",
                    Activity = "Created User",
                    Status = "Success"
                },

                new AuditLogItemViewModel
                {
                    DateTime = DateTime.Now.AddMinutes(-30),
                    User = "student1@test.com",
                    Module = "Proposal",
                    Activity = "Submitted Proposal",
                    Status = "Success"
                },

                new AuditLogItemViewModel
                {
                    DateTime = DateTime.Now.AddHours(-1),
                    User = "supervisor@test.com",
                    Module = "Reviews",
                    Activity = "Approved Proposal",
                    Status = "Success"
                },

                new AuditLogItemViewModel
                {
                    DateTime = DateTime.Now.AddHours(-2),
                    User = "admin@test.com",
                    Module = "Categories",
                    Activity = "Deleted Category",
                    Status = "Warning"
                },

                new AuditLogItemViewModel
                {
                    DateTime = DateTime.Now.AddHours(-3),
                    User = "system",
                    Module = "Authentication",
                    Activity = "Failed Login Attempt",
                    Status = "Error"
                },

                new AuditLogItemViewModel
                {
                    DateTime = DateTime.Now.AddHours(-5),
                    User = "committee@test.com",
                    Module = "Publication",
                    Activity = "Approved Publication",
                    Status = "Success"
                },

                new AuditLogItemViewModel
                {
                    DateTime = DateTime.Now.AddHours(-6),
                    User = "admin@test.com",
                    Module = "System Settings",
                    Activity = "Updated Configuration",
                    Status = "Success"
                }
            });

            return View(model);
        }
    }
}