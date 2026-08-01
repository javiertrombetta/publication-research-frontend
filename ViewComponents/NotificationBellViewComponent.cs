using Microsoft.AspNetCore.Mvc;
using ResearchPublicationManagementSystem.Infrastructure.Api;

namespace ResearchPublicationManagementSystem.ViewComponents
{
    /// <summary>
    /// The top bar's bell and its unread count.
    ///
    /// A view component rather than something every controller has to remember to populate: the
    /// bell is on every page, and a count that only a few controllers set would be silently wrong
    /// on the rest, which is worse than not showing one at all.
    /// </summary>
    public class NotificationBellViewComponent(NotificationsApiClient notificationsApi) : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            // A failure shows a plain bell rather than an error: the top bar appears on every
            // page, and a hiccup counting notifications must not disfigure whatever the person
            // actually came to do.
            var result = await notificationsApi.GetUnreadCountAsync();
            return View(result.Success ? result.Data : 0);
        }
    }
}
