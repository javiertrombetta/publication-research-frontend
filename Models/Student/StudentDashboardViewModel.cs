using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>Listing of every publication the student has started. A student may run several at once.</summary>
    public class StudentDashboardViewModel
    {
        /// <summary>The publications actually shown — after the search and sort below.</summary>
        public IReadOnlyList<PublicationContainerDto> Publications { get; set; } = [];

        /// <summary>How many the student has in total, so the view can say "3 of 7" when filtering.</summary>
        public int TotalCount { get; set; }

        public string? Query { get; set; }

        public string Sort { get; set; } = PublicationSort.DateNewest;

        public bool HasQuery => !string.IsNullOrWhiteSpace(Query);

        /// <summary>
        /// Above this many publications the list stops fitting on a screen at a glance and the
        /// search/sort tools start earning their space. The server can't know the viewport
        /// height, so this is a fixed approximation: cards sit two per row, so this is roughly
        /// three rows.
        /// </summary>
        public const int SearchToolsThreshold = 6;

        /// <summary>
        /// Hidden for short lists to keep the page clean, but always shown while a search is
        /// active — otherwise a query that filters down to a couple of results would remove
        /// the very control needed to clear it.
        /// </summary>
        public bool ShowSearchTools => HasQuery || TotalCount > SearchToolsThreshold;

        /// <summary>
        /// True when the list could not be loaded (backend unreachable, expired session) — as
        /// opposed to the student genuinely not having started any yet. Keeps the view from
        /// inviting them to create a publication they may already have.
        /// </summary>
        public bool LoadFailed { get; set; }
    }

    /// <summary>Sort keys accepted by the dashboard, kept as constants so the view and controller agree.</summary>
    public static class PublicationSort
    {
        public const string DateNewest = "date_desc";
        public const string DateOldest = "date_asc";
        public const string Title = "title";
        public const string Status = "status";
    }
}
