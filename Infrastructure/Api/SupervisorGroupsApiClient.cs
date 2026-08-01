using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

/// <summary>
/// A coordinator's saved sets of supervisors, and the administrator's view across everybody's.
/// </summary>
public class SupervisorGroupsApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<IReadOnlyList<SupervisorGroupDto>>> GetMineAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<SupervisorGroupDto>>("api/supervisor-groups", ct);

    public Task<ApiResult<SupervisorGroupDto>> CreateAsync(
        string name, IReadOnlyList<Guid> supervisorIds, CancellationToken ct = default) =>
        PostJsonAsync<SupervisorGroupDto>(
            "api/supervisor-groups", new SaveSupervisorGroupRequestDto(name, supervisorIds), ct);

    public Task<ApiResult<SupervisorGroupDto>> UpdateAsync(
        Guid groupId, string name, IReadOnlyList<Guid> supervisorIds, CancellationToken ct = default) =>
        PutJsonAsync<SupervisorGroupDto>(
            $"api/supervisor-groups/{groupId}", new SaveSupervisorGroupRequestDto(name, supervisorIds), ct);

    public Task<ApiResult<object>> DeleteAsync(Guid groupId, CancellationToken ct = default) =>
        DeleteAsync<object>($"api/supervisor-groups/{groupId}", ct);

    // ---------- The administrator's ----------

    public Task<ApiResult<IReadOnlyList<SupervisorGroupDto>>> GetAllAsync(
        string? search = null, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<SupervisorGroupDto>>(
            $"api/supervisor-groups/all{(string.IsNullOrWhiteSpace(search) ? "" : $"?search={Uri.EscapeDataString(search)}")}",
            ct);

    /// <summary>Edits any coordinator's group. The group stays with its owner.</summary>
    public Task<ApiResult<SupervisorGroupDto>> UpdateAnyAsync(
        Guid groupId, string name, IReadOnlyList<Guid> supervisorIds, CancellationToken ct = default) =>
        PutJsonAsync<SupervisorGroupDto>(
            $"api/supervisor-groups/{groupId}/any", new SaveSupervisorGroupRequestDto(name, supervisorIds), ct);

    /// <summary>Discards the groups named, or every group when <paramref name="all"/> is true.</summary>
    public Task<ApiResult<int>> DeleteManyAsync(
        IReadOnlyList<Guid> groupIds, bool all = false, CancellationToken ct = default) =>
        PostJsonAsync<int>(
            "api/supervisor-groups/delete", new DeleteSupervisorGroupsRequestDto(groupIds, all), ct);
}
