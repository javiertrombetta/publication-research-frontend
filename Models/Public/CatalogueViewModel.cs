using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>The public catalogue listing: what was searched for, and what came back.</summary>
    public class CatalogueViewModel
    {
        public CatalogueSearchQuery Search { get; set; } = new();

        public PagedResultDto<CatalogueEntryDto> Results { get; set; } =
            new([], 1, CatalogueSearchQuery.DefaultPageSize, 0);

        /// <summary>
        /// True when the catalogue itself could not be reached, which is different from a search
        /// that legitimately found nothing — the two need different wording.
        /// </summary>
        public bool LoadFailed { get; set; }

        /// <summary>
        /// The current filters as route values, so paging links keep the search the visitor made
        /// instead of silently resetting it.
        /// </summary>
        public Dictionary<string, string?> RouteValues(int? page = null)
        {
            var values = new Dictionary<string, string?>();

            void Add(string key, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value)) values[key] = value;
            }

            Add("query", Search.Query);
            Add("author", Search.Author);
            Add("supervisor", Search.Supervisor);
            Add("keyword", Search.Keyword);
            Add("publicationType", Search.PublicationType);
            Add("department", Search.Department);
            Add("researchArea", Search.ResearchArea);
            Add("year", Search.Year?.ToString());

            if (page is not null && page > 1) values["page"] = page.Value.ToString();

            return values;
        }
    }

    /// <summary>One published paper, plus the citations offered alongside it.</summary>
    public class CatalogueEntryViewModel
    {
        public CatalogueEntryDto Entry { get; set; } = null!;

        /// <summary>Null when the citation couldn't be built — the page is still worth showing.</summary>
        public CitationDto? Citation { get; set; }
    }
}
