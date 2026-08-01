namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// One clickable column heading.
    ///
    /// The sort travels in the query string, like the page number and the filters, so a sorted view
    /// can be linked to, reloaded and bookmarked, and it works without JavaScript. It has to: the
    /// ordering happens in the database, before the page is cut, because sorting the ten rows a
    /// page happens to hold is not sorting the list. The oldest proposal in a department sits on
    /// the last page, and a reader who asks for oldest first expects to see it.
    /// </summary>
    public class SortableColumnViewModel
    {
        public required string Controller { get; init; }
        public required string Action { get; init; }

        /// <summary>The name the API knows this column by.</summary>
        public required string Column { get; init; }

        public required string Label { get; init; }

        /// <summary>Which column the list is sorted by now, and which way, if any.</summary>
        public string? CurrentSort { get; init; }
        public bool CurrentDescending { get; init; }

        /// <summary>
        /// Which way this column goes when it is not the one being sorted by. Text reads naturally
        /// A to Z; a date reads more usefully newest first, which is what a reader opening a queue
        /// wants to see before they ask for anything.
        /// </summary>
        public bool DescendingFirst { get; init; }

        /// <summary>The rest of the screen's state, so sorting does not drop the search or filter.</summary>
        public Dictionary<string, string?> RouteValues { get; init; } = [];

        public bool IsActive =>
            string.Equals(CurrentSort, Column, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Clicking the column being sorted by reverses it. Clicking any other starts it at its own
        /// natural direction rather than inheriting whichever way the previous column happened to
        /// be pointing.
        /// </summary>
        public bool NextDescending => IsActive ? !CurrentDescending : DescendingFirst;

        public Dictionary<string, string?> RouteValuesForToggle()
        {
            var values = new Dictionary<string, string?>(RouteValues)
            {
                ["sort"] = Column
            };

            if (NextDescending) values["desc"] = "true";
            else values.Remove("desc");

            // Back to the first page: the rows that were on page three under the old order are not
            // the rows on page three under the new one, so keeping the number would land the reader
            // somewhere arbitrary.
            values.Remove("page");

            return values;
        }
    }
}
