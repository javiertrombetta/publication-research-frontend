using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

public class DepartmentsApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<IReadOnlyList<DepartmentDto>>> GetAllAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<DepartmentDto>>("api/departments", ct);
}
