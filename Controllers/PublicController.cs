using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Models;

namespace ResearchPublicationManagementSystem.Controllers
{
    /// <summary>
    /// The published catalogue. Deliberately anonymous — this is the one part of the system meant
    /// to be read by people who have no account, which is why it overrides the application's
    /// authenticate-by-default policy. Only papers whose author chose to publish them appear here;
    /// the backend enforces that independently.
    /// </summary>
    [AllowAnonymous]
    public class PublicController(
        CatalogueApiClient catalogueApi,
        Services.IInstitutionDetails institution) : Controller
    {
        /// <summary>
        /// Sends an anonymous visitor to sign in when there is no public catalogue to show them.
        ///
        /// The API refuses these requests as well — this only saves the visitor a page reporting a
        /// failure that is not a failure. Signed-in people are unaffected: the setting governs the
        /// public catalogue, not the institution's own access to what it has published.
        /// </summary>
        private async Task<IActionResult?> RedirectIfNotPublicAsync(CancellationToken cancellationToken)
        {
            if (User.Identity?.IsAuthenticated == true) return null;

            return (await institution.GetAsync(cancellationToken)).PublicCatalogueEnabled
                ? null
                : RedirectToAction("home", "Auth");
        }

        [HttpGet]
        public async Task<IActionResult> public_catalogue([FromQuery] CatalogueSearchQuery search, CancellationToken cancellationToken)
        {
            if (await RedirectIfNotPublicAsync(cancellationToken) is { } redirect) return redirect;

            search.Page = Math.Max(1, search.Page);
            search.PageSize = CatalogueSearchQuery.DefaultPageSize;

            var model = new CatalogueViewModel { Search = search };

            var result = await catalogueApi.SearchAsync(search);
            if (!result.Success || result.Data is null)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "The catalogue is unavailable right now.";
                model.LoadFailed = true;
            }
            else
            {
                model.Results = result.Data;
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> published_detail(Guid id, CancellationToken cancellationToken)
        {
            if (await RedirectIfNotPublicAsync(cancellationToken) is { } redirect) return redirect;

            var result = await catalogueApi.GetByIdAsync(id);
            if (!result.Success || result.Data is null)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "That publication isn't in the catalogue.";
                return RedirectToAction(nameof(public_catalogue));
            }

            var model = new CatalogueEntryViewModel { Entry = result.Data };

            // The page is worth showing even if the citation service hiccups.
            var citation = await catalogueApi.GetCitationAsync(id);
            model.Citation = citation.Data;

            return View(model);
        }
    }
}
