using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Models;

namespace ResearchPublicationManagementSystem.Controllers
{
    /// <summary>
    /// The signed-in person's notifications. Every role has them, so this is not scoped to one.
    ///
    /// A page rather than a dropdown: a notification is often the only record that something is
    /// waiting on you, and it needs room for who it came from, when, and a way through to the
    /// thing itself.
    /// </summary>
    [Authorize]
    public class NotificationsController(NotificationsApiClient notificationsApi) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index(bool unreadOnly = false, string? search = null, int page = 1)
        {
            var model = new NotificationsViewModel { UnreadOnly = unreadOnly, Search = search, Page = page };

            var result = await notificationsApi.GetAsync(unreadOnly, search, page);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage ?? "Could not load your notifications.";
                model.LoadFailed = true;
                return View(model);
            }

            var listing = result.Data;
            model.Notifications = listing?.Items ?? [];
            model.Page = listing?.Page ?? 1;
            model.TotalPages = listing?.TotalPages ?? 1;
            model.MatchingCount = listing?.TotalCount ?? 0;

            // Asked separately because a page cannot answer it. Counting the unread ones on screen
            // gave "3 unread" to somebody with sixty, and none at all to anybody reading page two.
            var unread = await notificationsApi.GetUnreadCountAsync();
            model.UnreadCount = unread.Success ? unread.Data : 0;

            return View(model);
        }

        /// <summary>
        /// Marks one as read and goes to what it is about. Reading and following are one action
        /// because they are one intention: opening a notification is what reading it means.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Open(Guid id)
        {
            // Where this goes is read from the notification itself rather than taken from the URL.
            // The old form trusted query parameters for the destination, which meant the link could
            // be pointed anywhere the person fancied. Harmless, since every target enforces its own
            // access, but the record is the only honest source.
            //
            // Fetched by id rather than looked for in a listing. That worked while the listing was
            // everything a person had; now that it is one page, opening anything below the tenth
            // would have quietly landed back here.
            var found = await notificationsApi.GetOneAsync(id);
            var notification = found.Data;

            if (notification is null)
            {
                return RedirectToAction(nameof(Index));
            }

            var read = await notificationsApi.MarkAsReadAsync(id);
            if (!read.Success)
            {
                TempData["ErrorMessage"] = read.ErrorMessage ?? "Could not open that notification.";
                return RedirectToAction(nameof(Index));
            }

            var destination = DestinationFor(notification.RelatedEntityType, notification.RelatedEntityId);
            return destination is null ? RedirectToAction(nameof(Index)) : Redirect(destination);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var result = await notificationsApi.MarkAllAsReadAsync();

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Everything is marked as read."
                : result.ErrorMessage ?? "Could not mark your notifications as read.";

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Where a notification points. Only the entity types that have a screen a person can act
        /// on are mapped; anything else keeps them on the list rather than sending them to a URL
        /// that does not exist.
        /// </summary>
        private string? DestinationFor(string? entityType, Guid? entityId)
        {
            if (entityId is not { } id)
            {
                return null;
            }

            return entityType switch
            {
                // A publication container is the one thing every role reaches differently, so it
                // goes to whatever that role's own view of it is.
                "PublicationContainer" => ContainerDestinationFor(id),
                "Committee" => Url.Action("committee_review", "ExternalSupervisor"),

                // Somebody wrote to this person. The id is the publication's rather than the
                // message's, because the screen is per publication: there is no screen showing one
                // message, and a conversation is the smallest thing worth opening.
                "ContainerMessages" => Url.Action("Publication", "Messages", new { id }),
                _ => null
            };
        }

        private string? ContainerDestinationFor(Guid containerId)
        {
            if (User.IsInRole(RoleNames.Student))
            {
                return Url.Action("Publication", "Student", new { id = containerId });
            }

            if (User.IsInRole(RoleNames.Supervisor))
            {
                return Url.Action("SupervisorDashboard", "Supervisor");
            }

            if (User.IsInRole(RoleNames.Coordinator))
            {
                return Url.Action("Coordinator_dashboard", "Coordinator");
            }

            if (User.IsInRole(RoleNames.HeadOfDepartment))
            {
                return Url.Action("Headofdepartment_feedback", "HeadOfDepartment", new { id = containerId });
            }

            return null;
        }
    }
}
