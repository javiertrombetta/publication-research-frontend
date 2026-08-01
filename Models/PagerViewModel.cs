namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// What the shared pager needs to draw itself and to link to its neighbours.
    ///
    /// The page number travels in the query string, as search and filter state does elsewhere here,
    /// so a page can be linked to, reloaded and bookmarked, and works without JavaScript.
    /// </summary>
    public class PagerViewModel
    {
        public required string Controller { get; init; }
        public required string Action { get; init; }
        public required int Page { get; init; }
        public required int TotalPages { get; init; }

        /// <summary>Anything else the screen filters by, carried across so paging keeps the view.</summary>
        public Dictionary<string, string?> RouteValues { get; init; } = [];

        /// <summary>
        /// The query key this pager turns. "page" for the one list a screen usually has; a screen
        /// with two lists side by side gives the second its own key, or turning one page would
        /// turn the other with it.
        /// </summary>
        public string PageKey { get; init; } = "page";

        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;

        public Dictionary<string, string?> RouteValuesFor(int page)
        {
            var values = new Dictionary<string, string?>(RouteValues);
            if (page > 1) values[PageKey] = page.ToString();
            else values.Remove(PageKey);
            return values;
        }
    }
}
