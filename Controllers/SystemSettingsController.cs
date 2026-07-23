using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Models;

namespace ResearchPublicationManagementSystem.Controllers
{
    public class SystemSettingsController : Controller
    {
        public IActionResult Index()
        {
            var model = new SystemSettingsViewModel
            {
                // General Settings
                SystemName = "Research Publication Management System",
                InstitutionName = "Auckland Institute of Studies",
                AcademicYear = "2026",

                // Proposal Settings
                MaximumProposalSubmission = 3,
                SupervisorPreferenceLimit = 3,
                AllowProposalRevisions = true,

                // Ethics Settings
                EnableEthicsApproval = true,
                RequiredEthicsForms = 3,

                // Upload Settings
                MaximumUploadFileSize = 50,
                AllowedFileTypes = "PDF, DOCX",

                // Notification Settings
                NotifyStudent = true,
                NotifySupervisor = true,
                NotifyResearchCoordinator = true,
                NotifyCommitteeMembers = true,
                NotifyHeadOfDepartment = true,
                NotifyPublicVisitor = true
            };

            return View(model);
        }
    }
}
