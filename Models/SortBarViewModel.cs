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

        public SortBarViewModel SortBar => new()
        {
            Controller = SortController,
            Action = SortAction,
            Sort = Sort,
            Descending = Descending,
            RouteValues = HasSearch ? new Dictionary<string, string?> { ["search"] = Search } : [],
            Columns =
            [
                ("started", "Date", true),
                ("student", "Student", false)
            ]
        };
    }

    /// <summary>
    /// The ordering control for one listing.
    ///
    /// Most of these queues are drawn as cards rather than tables, because a row carries an
    /// abstract, a set of replies and the buttons to act on them, which is more than a cell holds.
    /// A card has no header row to click, so the columns are named in a bar above the list instead.
    /// It is the same control either way: the same query keys, the same links, the same partial
    /// per column.
    /// </summary>
    public class SortBarViewModel
    {
        public required string Controller { get; init; }
        public required string Action { get; init; }

        public string? Sort { get; init; }
        public bool Descending { get; init; }

        /// <summary>Anything else the screen is filtered by, so ordering does not drop it.</summary>
        public Dictionary<string, string?> RouteValues { get; init; } = [];

        /// <summary>Column name as the API knows it, the label to show, and its natural direction.</summary>
        public List<(string Column, string Label, bool DescendingFirst)> Columns { get; init; } = [];

        /// <summary>Which query keys this bar writes, for a screen that has two lists.</summary>
        public string SortKey { get; init; } = "sort";
        public string DescendingKey { get; init; } = "desc";
        public string PageKey { get; init; } = "page";

        public IEnumerable<SortableColumnViewModel> Build() =>
            Columns.Select(c => new SortableColumnViewModel
            {
                Controller = Controller,
                Action = Action,
                Column = c.Column,
                Label = c.Label,
                CurrentSort = Sort,
                CurrentDescending = Descending,
                DescendingFirst = c.DescendingFirst,
                RouteValues = RouteValues,
                SortKey = SortKey,
                DescendingKey = DescendingKey,
                PageKey = PageKey
            });
    }
}
