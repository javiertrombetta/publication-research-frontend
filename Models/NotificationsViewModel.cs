using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>The signed-in person's notifications.</summary>
    public class NotificationsViewModel
    {
        public IReadOnlyList<NotificationDto> Notifications { get; set; } = [];

        /// <summary>
        /// Whether the list is filtered to unread. Kept in the query string rather than in
        /// session so the filtered view can be linked to and survives a refresh.
        /// </summary>
        public bool UnreadOnly { get; set; }

        public bool LoadFailed { get; set; }

        public int UnreadCount => Notifications.Count(n => !n.IsRead);
    }
}
