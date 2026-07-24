using Microsoft.AspNetCore.Mvc;

namespace MyWebApp.Controllers
{
    public class AuthController : Controller
    {
        // GET: /Auth/Login
        [HttpGet]
        public IActionResult home()
        {
            // ASP.NET automatically looks for Views/Auth/login.cshtml
            return View();
        }

        // GET: /Auth/CreateAccount
        [HttpGet]
        public IActionResult passwordrecovery()
        {
            // ASP.NET automatically looks for Views/Auth/createaccount.cshtml
            return View();
        }

        public IActionResult passwordreset()
        {
            
            return View();
        }

         public IActionResult signup()
        {
            
            return View();
        }
    }
}