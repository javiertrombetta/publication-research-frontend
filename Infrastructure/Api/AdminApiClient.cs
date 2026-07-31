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
        AuditLogQuery query, CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["entityType"] = string.IsNullOrWhiteSpace(query.EntityType) ? null : query.EntityType,
            ["userId"] = query.UserId?.ToString(),
            ["from"] = query.From?.ToString("O"),
            ["to"] = query.To?.ToString("O"),
            ["page"] = query.Page.ToString(),
            ["pageSize"] = query.PageSize.ToString()
        };

        var url = QueryHelpers.AddQueryString("api/audit-log",
            parameters.Where(p => !string.IsNullOrWhiteSpace(p.Value)));

        return GetAsync<PagedResultDto<AuditLogEntryDto>>(url, ct);
    }

    /// <summary>How many committee members of each type a publication needs by default.</summary>
    public Task<ApiResult<IReadOnlyList<CommitteeRoleConfigDto>>> GetDefaultCommitteeConfigAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<CommitteeRoleConfigDto>>("api/settings/default-committee", ct);

    public Task<ApiResult<object?>> SetDefaultCommitteeConfigAsync(
        SetCommitteeRoleConfigRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<object?>("api/settings/default-committee", request, ct);
}

/// <summary>Audit-log filters, carried in the query string so a filtered view can be linked to.</summary>
public class AuditLogQuery
{
    public const int DefaultPageSize = 25;

    public Guid? UserId { get; set; }
    public string? EntityType { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;

    public bool HasFilters =>
        UserId is not null || !string.IsNullOrWhiteSpace(EntityType) || From is not null || To is not null;
}
