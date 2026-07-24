using Microsoft.AspNetCore.Mvc;

namespace MyWebApp.Controllers
{
    public class StudentController : Controller
    {
       
        [HttpGet]
        public IActionResult Create_proposals()
        {
            
            return View();
        }

          public IActionResult Create_Publication()
        {
            
            return View();
        }

        public IActionResult Edit_studentprofile()
        {
            
            return View();
        }

       

         public IActionResult Ethic_risk_assessment()
        {
            
            return View();
        }

         public IActionResult student_dashboard()
        {
            
            return View();
        }

         public IActionResult studentprofile()
        {
            
            return View();
        }

         public IActionResult Upload_Ethic_file()
        {
            
            return View();
        }

        

        
    }
}