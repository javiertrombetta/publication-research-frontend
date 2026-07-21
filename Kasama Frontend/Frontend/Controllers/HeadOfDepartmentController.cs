using Microsoft.AspNetCore.Mvc;

namespace MyWebApp.Controllers
{
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