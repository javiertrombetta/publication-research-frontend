using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Common;

namespace ResearchPublicationManagementSystem.Controllers
{
    [Authorize(Roles = RoleNames.HeadOfDepartment)]
    public class HeadOfDepartmentController : Controller
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

         public IActionResult all_proposals_fromstudent()
        {
            
            return View();
        }

          public IActionResult Head_of_Department_dashboard()
        {
            
            return View();
        }

          public IActionResult Headofdepartment_feedback()
        {
            
            return View();
        }
        
    }
}