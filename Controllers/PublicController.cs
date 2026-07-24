using Microsoft.AspNetCore.Mvc;

namespace MyWebApp.Controllers
{
    public class PublicController : Controller
    {
         
        [HttpGet]
        public IActionResult public_catalogue()
        {
            
            return View();
        }

         public IActionResult published_detail()
        {
            
            return View();
        }      
        
    }
}