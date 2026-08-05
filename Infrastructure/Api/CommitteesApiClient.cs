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

    /// <summary>Whether the person signed in could be put on a committee under the rules as they stand.</summary>
    public Task<ApiResult<bool>> GetMyEligibilityAsync(CancellationToken ct = default) =>
        GetAsync<bool>("api/committees/my-eligibility", ct);

    /// <summary>Every committee still sitting, so an administrator can find one to change.</summary>
    public Task<ApiResult<PagedResultDto<CommitteeDto>>> GetInProgressAsync(
        int page = 1, int pageSize = Paging.AsConfigured, CancellationToken ct = default) =>
        GetAsync<PagedResultDto<CommitteeDto>>(
            $"api/committees/in-progress?page={Math.Max(1, page)}{Paging.SizeParam(pageSize)}", ct);

    /// <summary>Changes who sits on a committee. Refused once it has finished.</summary>
    public Task<ApiResult<CommitteeDto>> UpdateAsync(
        Guid committeeId, UpdateCommitteeRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<CommitteeDto>($"api/committees/{committeeId}", request, ct);

    /// <summary>The committees the acting member sits on.</summary>
    /// <param name="awaitingMe">
    /// Narrows it to the papers this member has still to vote on. Asked for with the shortest page
    /// there is when only the figure is wanted, which is what the dashboard's card needs.
    /// </param>
    public Task<ApiResult<PagedResultDto<CommitteeDto>>> GetMyAssignmentsAsync(
        int page = 1, int pageSize = Paging.AsConfigured, string? sort = null, bool descending = false,
        string? search = null, bool awaitingMe = false, CancellationToken ct = default)
    {
        var query = $"api/committees/my-assignments?page={Math.Max(1, page)}{Paging.SizeParam(pageSize)}";
        if (!string.IsNullOrWhiteSpace(sort)) query += $"&sortBy={Uri.EscapeDataString(sort)}";
        if (descending) query += "&sortDescending=true";
        if (!string.IsNullOrWhiteSpace(search)) query += $"&search={Uri.EscapeDataString(search.Trim())}";
        if (awaitingMe) query += "&awaitingMe=true";

        return GetAsync<PagedResultDto<CommitteeDto>>(query, ct);
    }


    public Task<ApiResult<object?>> MemberReviewAsync(
        Guid committeeId, CommitteeMemberReviewRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/committees/{committeeId}/review", request, ct);

    public Task<ApiResult<CommitteeDto>> AssignAsync(
        Guid publicationId, AssignCommitteeRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<CommitteeDto>($"api/publications/{publicationId}/assign-committee", request, ct);
}
