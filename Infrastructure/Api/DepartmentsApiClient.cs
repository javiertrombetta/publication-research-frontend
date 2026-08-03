using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

public class DepartmentsApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<IReadOnlyList<DepartmentDto>>> GetAllAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<DepartmentDto>>("api/departments", ct);

    public Task<ApiResult<DepartmentDto>> CreateAsync(CreateDepartmentRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<DepartmentDto>("api/departments", request, ct);

    public Task<ApiResult<DepartmentDto>> UpdateAsync(Guid id, UpdateDepartmentRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<DepartmentDto>($"api/departments/{id}", request, ct);

    public Task<ApiResult<object>> RemoveAsync(Guid id, CancellationToken ct = default) =>
        DeleteAsync<object>($"api/departments/{id}", ct);

    public Task<ApiResult<DepartmentMembersDto>> GetMembersAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<DepartmentMembersDto>($"api/departments/{id}/members", ct);

    public Task<ApiResult<DepartmentMembersDto>> SetMembersAsync(
        Guid id, SetDepartmentMembersRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<DepartmentMembersDto>($"api/departments/{id}/members", request, ct);
}
