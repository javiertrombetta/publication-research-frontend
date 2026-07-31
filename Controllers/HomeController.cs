using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Models;

namespace ResearchPublicationManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Entry point for "/". The published catalogue is the front door of the site, for
        /// visitors and signed-in users alike: it is the part meant to be read by anyone, and it
        /// is what the institution is publishing the research for. Signing in is offered from the
        /// header, and each role still lands on its own dashboard after logging in.
        /// </summary>
        [AllowAnonymous]
        public IActionResult Index()
        {
            var (controller, action) = RoleLanding.Anonymous;
            return RedirectToAction(action, controller);
        }

        /// <summary>Linked from the footer, which is shown on the signed-out pages too.</summary>
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
