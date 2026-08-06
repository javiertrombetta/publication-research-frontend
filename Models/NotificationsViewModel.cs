using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>The signed-in person's notifications, one page of them.</summary>
    public class NotificationsViewModel
    {
        public IReadOnlyList<NotificationDto> Notifications { get; set; } = [];

        /// <summary>
        /// Whether the list is filtered to unread. Kept in the query string rather than in
        /// session so the filtered view can be linked to and survives a refresh.
        /// </summary>
        public bool UnreadOnly { get; set; }

        /// <summary>
        /// What the reader typed. Applied by the API across the whole list rather than to the page
        /// already on screen, so it finds the notification from three months ago too.
        /// </summary>
        public string? Search { get; set; }

        public int Page { get; set; } = 1;
        public int TotalPages { get; set; } = 1;

        /// <summary>Everything this person has, before the tab and the search narrowed it.</summary>
        public int MatchingCount { get; set; }

        public bool LoadFailed { get; set; }

        /// <summary>
        /// How many are unread, across the whole list rather than this page. Counted by the API's
        /// own endpoint: a page of ten cannot answer it, and the answer is what the tab and the
        /// "mark all as read" button are about.
        /// </summary>
        public int UnreadCount { get; set; }

        public bool IsSearching => !string.IsNullOrWhiteSpace(Search);

        /// <summary>
        /// The state a link out of here has to carry: which tab and what was searched for. The page
        /// number is not among them, because everything that uses this is changing the page or
        /// starting over.
        /// </summary>
        public Dictionary<string, string?> RouteValues()
        {
            var values = new Dictionary<string, string?>();
            if (UnreadOnly) values["unreadOnly"] = "true";
            if (IsSearching) values["search"] = Search;
            return values;
        }

        /// <summary>The same view with the search dropped, for the Clear button.</summary>
        public Dictionary<string, string?> ClearSearchRoute()
        {
            var values = new Dictionary<string, string?>();
            if (UnreadOnly) values["unreadOnly"] = "true";
            return values;
        }

        public PagerViewModel Pager() => new()
        {
            Controller = "Notifications",
            Action = "Index",
            Page = Page,
            TotalPages = TotalPages,
            RouteValues = RouteValues()
        };
    }
}
