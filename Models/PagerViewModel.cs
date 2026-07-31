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

        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;

        public Dictionary<string, string?> RouteValuesFor(int page)
        {
            var values = new Dictionary<string, string?>(RouteValues);
            if (page > 1) values["page"] = page.ToString();
            else values.Remove("page");
            return values;
        }
    }
}
