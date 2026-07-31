using Microsoft.AspNetCore.WebUtilities;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

public class ContainersApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<PublicationContainerDto>> CreateAsync(CancellationToken ct = default) =>
        PostAsync<PublicationContainerDto>("api/containers", ct);

    /// <summary>All of the acting student's publications, newest first. Empty list when they haven't started any.</summary>
    public Task<ApiResult<IReadOnlyList<PublicationContainerDto>>> GetMineAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PublicationContainerDto>>("api/containers/me", ct);

    public Task<ApiResult<PublicationContainerDto>> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<PublicationContainerDto>($"api/containers/{id}", ct);

    /// <summary>Discards one of the student's own publications; the backend rejects it once it holds proposals.</summary>
    public Task<ApiResult<object?>> DeleteAsync(Guid id, CancellationToken ct = default) =>
        DeleteAsync<object?>($"api/containers/{id}", ct);

    public Task<ApiResult<IReadOnlyList<ActivityHistoryEntryDto>>> GetActivityHistoryAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ActivityHistoryEntryDto>>($"api/containers/{id}/activity-history", ct);

    /// <summary>
    /// Containers filtered server-side. A Coordinator passes their own id so the listing is
    /// their workload rather than the whole institution's.
    /// </summary>
    public Task<ApiResult<IReadOnlyList<PublicationContainerDto>>> GetAllAsync(
        Guid? studentId = null, Guid? coordinatorId = null, string? status = null, CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["studentId"] = studentId?.ToString(),
            ["coordinatorId"] = coordinatorId?.ToString(),
            ["status"] = status
        };

        var url = QueryHelpers.AddQueryString("api/containers",
            parameters.Where(p => !string.IsNullOrWhiteSpace(p.Value)));

        return GetAsync<IReadOnlyList<PublicationContainerDto>>(url, ct);
    }

    /// <summary>The publications this supervisor has been assigned to, newest first.</summary>
    public Task<ApiResult<IReadOnlyList<PublicationContainerDto>>> GetSupervisingAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PublicationContainerDto>>("api/containers/supervising", ct);

    /// <summary>Every publication by a student in this Head of Department's department.</summary>
    public Task<ApiResult<IReadOnlyList<PublicationContainerDto>>> GetInMyDepartmentAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PublicationContainerDto>>("api/containers/in-my-department", ct);
}
