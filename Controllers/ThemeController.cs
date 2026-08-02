using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Services;

namespace ResearchPublicationManagementSystem.Controllers
{
    /// <summary>
    /// Switching between the light and the dark theme.
    ///
    /// Open to everyone, signed in or not: the catalogue and the sign-in page are read by people
    /// with no account, and a preference about how a page looks is not something to hold behind
    /// one. Where there is an account, the choice is written to it as well, so it follows the
    /// person to another machine instead of staying with the browser.
    /// </summary>
    [AllowAnonymous]
    public class ThemeController(UsersApiClient usersApi) : Controller
    {
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Set(string theme, string? returnUrl)
        {
            var chosen = SiteTheme.Set(HttpContext, theme);

            // Best effort, and deliberately not reported. The cookie is what the page is drawn
            // from, so the switch has already worked by the time this runs; failing to reach the
            // API should not turn a change of colour into an error message.
            if (User.Identity?.IsAuthenticated == true)
            {
                await usersApi.SetThemeAsync(chosen);
            }

            // Back where they were, so the switch never moves anybody off the page they are
            // reading. Local addresses only: a returnUrl is user input.
            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction("Index", "Home");
        }
    }
}
