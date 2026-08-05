using Microsoft.AspNetCore.WebUtilities;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

/// <summary>
/// The institution-wide views only an administrator has: the summary dashboard, the audit trail
/// and the committee defaults.
/// </summary>
public class AdminApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<DashboardSummaryDto>> GetSummaryAsync(CancellationToken ct = default) =>
        GetAsync<DashboardSummaryDto>("api/dashboard/summary", ct);

    public Task<ApiResult<PagedResultDto<AuditLogEntryDto>>> GetAuditLogAsync(
        AuditLogQuery query, CancellationToken ct = default) =>
        GetAsync<PagedResultDto<AuditLogEntryDto>>(AuditLogUrl("api/audit-log", query), ct);

    /// <summary>
    /// The same trail as a CSV file, filtered and ordered exactly as the screen is. The whole of
    /// it, not the page in hand: somebody exporting a filtered view wants the filter's results,
    /// and handing them ten rows of it would be worse than useless.
    /// </summary>
    public Task<(byte[] Content, string ContentType, string FileName)?> ExportAuditLogAsync(
        AuditLogQuery query, CancellationToken ct = default) =>
        GetFileAsync(AuditLogUrl("api/audit-log/export", query), ct);

    /// <summary>The filters and ordering as a query string, so the page and the file always agree.</summary>
    private static string AuditLogUrl(string path, AuditLogQuery query)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["entityType"] = string.IsNullOrWhiteSpace(query.EntityType) ? null : query.EntityType,
            ["userId"] = query.UserId?.ToString(),
            ["from"] = query.From?.ToString("O"),
            ["to"] = query.To?.ToString("O"),
            ["page"] = query.Page.ToString(),
            ["pageSize"] = Common.Paging.SizeValue(query.PageSize)
        };

        if (!string.IsNullOrWhiteSpace(query.SortBy))
        {
            parameters["sortBy"] = query.SortBy;
            parameters["sortDescending"] = query.SortDescending ? "true" : "false";
        }

        return QueryHelpers.AddQueryString(path, parameters.Where(p => !string.IsNullOrWhiteSpace(p.Value)));
    }

}

/// <summary>Audit-log filters, carried in the query string so a filtered view can be linked to.</summary>
public class AuditLogQuery
{
    /// <summary>Nothing, so the API applies the length the institution has set.</summary>
    public const int DefaultPageSize = Common.Paging.AsConfigured;

    public Guid? UserId { get; set; }
    public string? EntityType { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;

    /// <summary>
    /// Which column the trail is ordered by, and which way. Applied by the API before the page is
    /// cut, so clicking a heading orders the whole trail rather than the twenty rows on screen.
    /// </summary>
    public string? SortBy { get; set; }

    /// <inheritdoc cref="SortBy"/>
    public bool SortDescending { get; set; }

    public bool HasFilters =>
        UserId is not null || !string.IsNullOrWhiteSpace(EntityType) || From is not null || To is not null;
}
