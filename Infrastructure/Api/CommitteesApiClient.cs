using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

/// <summary>
/// Evaluation committees. Assignment is an Admin action; the rest is what a committee member
/// needs to see and record their decision.
/// </summary>
public class CommitteesApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    /// <summary>The committees the acting member sits on.</summary>
    public Task<ApiResult<IReadOnlyList<CommitteeDto>>> GetMyAssignmentsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<CommitteeDto>>("api/committees/my-assignments", ct);

    public Task<ApiResult<CommitteeDto>> GetByPublicationAsync(Guid publicationId, CancellationToken ct = default) =>
        GetAsync<CommitteeDto>($"api/publications/{publicationId}/committee", ct);

    public Task<ApiResult<object?>> MemberReviewAsync(
        Guid committeeId, CommitteeMemberReviewRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/committees/{committeeId}/review", request, ct);

    public Task<ApiResult<CommitteeDto>> AssignAsync(
        Guid publicationId, AssignCommitteeRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<CommitteeDto>($"api/publications/{publicationId}/assign-committee", request, ct);
}
