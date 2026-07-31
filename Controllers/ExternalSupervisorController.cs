using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Common;

namespace ResearchPublicationManagementSystem.Controllers
{
    [Authorize(Roles = RoleNames.ExternalCommitteeMember)]
    public class ExternalSupervisorController : Controller
    {
         
        [HttpGet]
        public IActionResult committee_review()
        {
            
            return View();
        }

         public IActionResult staff_profile()
        {
            
            return View();
        }


         public IActionResult Edit_staff_profile()
        {
            
            return View();
        }

         public IActionResult External_Supervisor_Dashboard()
        {
            
            return View();
        }
        
    }
}