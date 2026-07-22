using Microsoft.AspNetCore.Mvc;

namespace MyWebApp.Controllers
{
    public class CoordinatorController : Controller
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

        public IActionResult Coordinator_dashboard()
        {
            
            return View();
        }

       

         public IActionResult Ethic_review_afters_headofdepartment()
        {
            
            return View();
        }

         public IActionResult Ethic_review_aftersupervisor()
        {
            
            return View();
        }

         public IActionResult Evaluation_after_committee()
        {
            
            return View();
        }

         public IActionResult select_a_proposal_forstudent()
        {
            
            return View();
        }

         public IActionResult staff_profile()
        {
            
            return View();
        }

        public IActionResult assigning_proposal_forsupervisor()
        {
            
            return View();
        }
    }
}