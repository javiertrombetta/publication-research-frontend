using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

public class ProposalsApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<ProposalDto>> CreateAsync(Guid containerId, SaveProposalRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<ProposalDto>($"api/containers/{containerId}/proposals", request, ct);

    public Task<ApiResult<ProposalDto>> UpdateAsync(Guid proposalId, SaveProposalRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<ProposalDto>($"api/proposals/{proposalId}", request, ct);

    public Task<ApiResult<IReadOnlyList<ProposalDto>>> GetByContainerAsync(Guid containerId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ProposalDto>>($"api/containers/{containerId}/proposals", ct);

    public Task<ApiResult<object?>> FinishSubmissionAsync(Guid containerId, CancellationToken ct = default) =>
        PostAsync<object?>($"api/containers/{containerId}/proposals/finish-submission", ct);
}
