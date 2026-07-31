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
        private readonly Services.IInstitutionDetails _institution;

        public HomeController(ILogger<HomeController> logger, Services.IInstitutionDetails institution)
        {
            _logger = logger;
            _institution = institution;
        }

        /// <summary>
        /// Entry point for "/", and which page that is is the administrator's decision.
        ///
        /// With a public catalogue, it is the front door: the part meant to be read by anyone, and
        /// what the institution is publishing the research for. Without one the site has no public
        /// face, so a visitor is shown the sign-in page instead of a catalogue that would refuse
        /// them. Either way each role lands on its own dashboard once signed in.
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            if (User.Identity?.IsAuthenticated != true &&
                !(await _institution.GetAsync(cancellationToken)).PublicCatalogueEnabled)
            {
                return RedirectToAction("home", "Auth");
            }

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
