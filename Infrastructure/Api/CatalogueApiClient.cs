using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

/// <summary>
/// The published catalogue. Every endpoint behind this client is anonymous on the backend, which
/// is why this is the one client registered without the bearer-token handler: a visitor who has
/// never signed in must be able to search and read.
/// </summary>
public class CatalogueApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<PagedResultDto<CatalogueEntryDto>>> SearchAsync(
        CatalogueSearchQuery query, CancellationToken ct = default)
    {
        // Only send the filters that are actually set — an empty string is a filter the backend
        // would honour, and it would match nothing.
        var parameters = new Dictionary<string, string?>
        {
            ["query"] = Trimmed(query.Query),
            ["author"] = Trimmed(query.Author),
            ["supervisor"] = Trimmed(query.Supervisor),
            ["keyword"] = Trimmed(query.Keyword),
            ["publicationType"] = Trimmed(query.PublicationType),
            ["department"] = Trimmed(query.Department),
            ["researchArea"] = Trimmed(query.ResearchArea),
            ["year"] = query.Year?.ToString(CultureInfo.InvariantCulture),
            ["page"] = query.Page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = query.PageSize.ToString(CultureInfo.InvariantCulture)
        };

        var url = QueryHelpers.AddQueryString("api/catalogue",
            parameters.Where(p => !string.IsNullOrWhiteSpace(p.Value)));

        return GetAsync<PagedResultDto<CatalogueEntryDto>>(url, ct);
    }

    public Task<ApiResult<CatalogueEntryDto>> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<CatalogueEntryDto>($"api/catalogue/{id}", ct);

    public Task<ApiResult<CitationDto>> GetCitationAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<CitationDto>($"api/catalogue/{id}/citation", ct);

    private static string? Trimmed(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>
/// The catalogue filters, carried in the query string so a search can be linked to, bookmarked
/// and reloaded, and so the whole page keeps working without JavaScript.
/// </summary>
public class CatalogueSearchQuery
{
    public const int DefaultPageSize = 10;

    public string? Query { get; set; }
    public string? Author { get; set; }
    public string? Supervisor { get; set; }
    public string? Keyword { get; set; }
    public string? PublicationType { get; set; }
    public string? Department { get; set; }
    public string? ResearchArea { get; set; }
    public int? Year { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(Query) || !string.IsNullOrWhiteSpace(Author) ||
        !string.IsNullOrWhiteSpace(Supervisor) || !string.IsNullOrWhiteSpace(Keyword) ||
        !string.IsNullOrWhiteSpace(PublicationType) || !string.IsNullOrWhiteSpace(Department) ||
        !string.IsNullOrWhiteSpace(ResearchArea) || Year is not null;
}
