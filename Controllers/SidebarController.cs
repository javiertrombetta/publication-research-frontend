using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Services;

namespace ResearchPublicationManagementSystem.Controllers
{
    /// <summary>
    /// Where somebody's own arrangement of the sidebar is recorded.
    ///
    /// Two places, for two different jobs. The account, so it is theirs and follows them to another
    /// machine; and the session, so every page from here draws the menu that way without asking the
    /// API for it each time.
    ///
    /// It used to be kept in the browser's own storage, which is a machine rather than a person: an
    /// arrangement one person made was handed to whoever signed in on that machine next.
    /// </summary>
    [Authorize]
    public class SidebarController(
        UsersApiClient usersApi,
        IAuthCookieService authCookie) : Controller
    {
        /// <summary>
        /// The routes of the menu's items, in the order this person wants them.
        ///
        /// Two shapes, because they arrive by two routes. Ordinarily a fetch sends a JSON array. A
        /// tab being closed sends a beacon instead, which carries no headers of its own and so has
        /// to be a form: the token travels in it, where the framework also looks.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Order()
        {
            var routes = (await ReadRoutesAsync())
                .Select(item => (item ?? string.Empty).Trim())
                .Where(item => item.Length > 0 && !item.Contains(' '))
                .Distinct()
                .ToArray();

            var result = await usersApi.SetSidebarOrderAsync(routes);
            if (!result.Success)
            {
                // Nothing is said to the reader. The menu is already in the order they put it in;
                // what has failed is remembering that for next time, and interrupting somebody
                // mid-task to say so would cost more than it is worth.
                return StatusCode(StatusCodes.Status502BadGateway);
            }

            await authCookie.SetSidebarOrderAsync(HttpContext, string.Join(' ', routes));
            return NoContent();
        }

        private async Task<IReadOnlyList<string?>> ReadRoutesAsync()
        {
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync();
                return form["items"].ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            }

            try
            {
                return await JsonSerializer.DeserializeAsync<string[]>(Request.Body) ?? [];
            }
            catch (JsonException)
            {
                // A body that is not a list of routes is not an arrangement. Nothing is recorded,
                // and nothing is said: there is no person waiting on an answer here.
                return [];
            }
        }
    }
}
