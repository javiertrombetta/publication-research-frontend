using Microsoft.AspNetCore.Mvc;

namespace MyWebApp.Controllers
{
    public class SupervisorController : Controller
    {
       
        [HttpGet]
        public IActionResult committee_review()
        {
            
            return View();
        }

          public IActionResult Edit_staff_profile()
        {
            
            return View();
        }

        public IActionResult staff_profile()
        {
            
            return View();
        }

        public IActionResult Ethic_document_review()
        {
            
            return View();
        }

       

         public IActionResult proposal_review()
        {
            
            return View();
        }

         public IActionResult publication_review()
        {
            
            return View();
        }

         public IActionResult Review_Ethic_assessmentchecklist()
        {
            
            return View();
        }

         public IActionResult SupervisorDashboard()
        {
            
            return View();
        }

         
    }
}