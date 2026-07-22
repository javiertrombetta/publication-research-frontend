using Microsoft.AspNetCore.Mvc;

namespace MyWebApp.Controllers
{
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