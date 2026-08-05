namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// The ordering a publication queue was asked for, and the bar that offers to change it.
    ///
    /// Shared by the screens that list publications: the coordinator's two ethics queues and the
    /// head of department's. They order by the same things, because they show the same rows.
    /// </summary>
    public abstract class SortablePublicationQueue
    {
        public string? Sort { get; set; }
        public bool Descending { get; set; }
        public string? Search { get; set; }
        public int TotalCount { get; set; }

        public bool HasSearch => !string.IsNullOrWhiteSpace(Search);

        protected abstract string SortController { get; }
        protected abstract string SortAction { get; }

        public Dictionary<string, string?> RouteValues()
        {
            var values = new Dictionary<string, string?>();
            if (!string.IsNullOrWhiteSpace(Sort)) values["sort"] = Sort;
            if (Descending) values["desc"] = "true";
            if (HasSearch) values["search"] = Search;
            return values;
        }

        /// <summary>
        /// Where Clear goes: this queue again, without the search term and still in the order the
        /// reader had chosen.
        /// </summary>
        public Dictionary<string, string?> ClearSearchRoute() =>
            RouteValues().Where(v => v.Key != "search").ToDictionary(v => v.Key, v => v.Value);

        /// <summary>
        /// One sortable heading for the row that stands above these cards. The search term travels
        /// with it and the ordering does not: the heading is what sets the ordering, so carrying
        /// the current one would fight with the click.
        /// </summary>
        public SortableColumnViewModel Column(string column, string label, bool descendingFirst = false) => new()
        {
            Controller = SortController,
            Action = SortAction,
            Column = column,
            Label = label,
            CurrentSort = Sort,
            CurrentDescending = Descending,
            DescendingFirst = descendingFirst,
            RouteValues = HasSearch ? new Dictionary<string, string?> { ["search"] = Search } : []
        };
    }
}
