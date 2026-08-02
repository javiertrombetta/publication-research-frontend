using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

/// <summary>
/// Evaluation committees. Assignment is an Admin action; the rest is what a committee member
/// needs to see and record their decision.
/// </summary>
public class CommitteesApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    /// <summary>
    /// Everybody who could be put on a committee right now. Asked for rather than worked out here,
    /// so the list offered and the list the API will accept are the same answer.
    /// </summary>
    public Task<ApiResult<IReadOnlyList<CommitteeCandidateDto>>> GetCandidatesAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<CommitteeCandidateDto>>("api/committees/candidates", ct);

    /// <summary>The committees the acting member sits on.</summary>
    public Task<ApiResult<PagedResultDto<CommitteeDto>>> GetMyAssignmentsAsync(
        int page = 1, int pageSize = Paging.DefaultPageSize, CancellationToken ct = default) =>
        GetAsync<PagedResultDto<CommitteeDto>>(
            $"api/committees/my-assignments?page={Math.Max(1, page)}&pageSize={pageSize}", ct);

    public Task<ApiResult<CommitteeDto>> GetByPublicationAsync(Guid publicationId, CancellationToken ct = default) =>
        GetAsync<CommitteeDto>($"api/publications/{publicationId}/committee", ct);

    public Task<ApiResult<object?>> MemberReviewAsync(
        Guid committeeId, CommitteeMemberReviewRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/committees/{committeeId}/review", request, ct);

    public Task<ApiResult<CommitteeDto>> AssignAsync(
        Guid publicationId, AssignCommitteeRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<CommitteeDto>($"api/publications/{publicationId}/assign-committee", request, ct);
}
