using ResearchPublicationManagementSystem.Common;
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

    // ---------- Coordinator ----------

    /// <summary>Submitted proposals of the acting Coordinator that no supervisor has been invited to yet.</summary>
    /// <summary>
    /// Every proposal in this coordinator's publications, with each supervisor's answer attached.
    /// Replaces walking the publications and asking per proposal, which cost a request a row.
    /// </summary>
    /// <param name="awaitingAllocation">
    /// Only the proposals a supervisor has offered to take on and nobody has been allocated to —
    /// what the selection screen can act on. Filtered by the API so a page of it is a page of that
    /// screen rather than of everything.
    /// </param>
    public Task<ApiResult<PagedResultDto<ProposalWithInvitationsDto>>> GetForCoordinatorAsync(
        int page = 1, bool awaitingAllocation = false, int pageSize = Paging.DefaultPageSize,
        CancellationToken ct = default) =>
        GetAsync<PagedResultDto<ProposalWithInvitationsDto>>(
            $"api/proposals/for-coordinator?page={Math.Max(1, page)}&pageSize={pageSize}&awaitingAllocation={awaitingAllocation}", ct);

    /// <summary>Every proposal from the students of the department this person heads.</summary>
    public Task<ApiResult<PagedResultDto<ProposalWithInvitationsDto>>> GetInMyDepartmentAsync(
        int page = 1, int pageSize = Paging.DefaultPageSize, CancellationToken ct = default) =>
        GetAsync<PagedResultDto<ProposalWithInvitationsDto>>(
            $"api/proposals/in-my-department?page={Math.Max(1, page)}&pageSize={pageSize}", ct);

    public Task<ApiResult<PagedResultDto<ProposalDto>>> GetPendingAsync(
        int page = 1, int pageSize = Paging.DefaultPageSize, CancellationToken ct = default) =>
        GetAsync<PagedResultDto<ProposalDto>>(
            $"api/proposals/pending?page={Math.Max(1, page)}&pageSize={pageSize}", ct);

    public Task<ApiResult<object?>> SendToSupervisorsAsync(SendToSupervisorsRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>("api/proposals/send-to-supervisors", request, ct);

    /// <summary>Which supervisors were invited to a proposal, and which of them accepted it.</summary>
    public Task<ApiResult<IReadOnlyList<SupervisorInvitationDto>>> GetSelectionsAsync(Guid proposalId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<SupervisorInvitationDto>>($"api/proposals/{proposalId}/selections", ct);

    public Task<ApiResult<object?>> AssignSupervisorAsync(Guid proposalId, AssignSupervisorRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/proposals/{proposalId}/assign-supervisor", request, ct);

    public Task<ApiResult<object?>> RequestResubmissionAsync(Guid containerId, CommentsRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/containers/{containerId}/proposals/request-resubmission", request, ct);

    public Task<ApiResult<object?>> DeferToNextCycleAsync(Guid containerId, CommentsRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/containers/{containerId}/proposals/defer-to-next-cycle", request, ct);

    // ---------- Supervisor ----------

    /// <summary>Proposals a Coordinator has sent this supervisor to consider.</summary>
    public Task<ApiResult<IReadOnlyList<ProposalDto>>> GetInvitedAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ProposalDto>>("api/proposals/invited", ct);

    /// <summary>Records that this supervisor is willing to supervise the proposal.</summary>
    public Task<ApiResult<object?>> SupervisorSelectionAsync(
        Guid proposalId, SupervisorSelectionRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/proposals/{proposalId}/supervisor-selection", request, ct);
}
