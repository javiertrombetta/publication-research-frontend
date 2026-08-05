using Microsoft.AspNetCore.Http;
using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

public class PublicationsApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<PublicationDto>> GetOrCreateDraftAsync(Guid containerId, CancellationToken ct = default) =>
        PostAsync<PublicationDto>($"api/containers/{containerId}/publications", ct);

    /// <summary>404 (ApiResult.StatusCode == 404) means the paper stage isn't unlocked yet (ethics not resolved).</summary>
    /// <summary>The paper by its own id, for anyone with access to its publication.</summary>
    public Task<ApiResult<PublicationDto>> GetByIdAsync(Guid publicationId, CancellationToken ct = default) =>
        GetAsync<PublicationDto>($"api/publications/{publicationId}", ct);

    public Task<ApiResult<PublicationDto>> GetByContainerAsync(Guid containerId, CancellationToken ct = default) =>
        GetAsync<PublicationDto>($"api/containers/{containerId}/publications", ct);

    public Task<ApiResult<PublicationDto>> UpdateMetadataAsync(Guid publicationId, UpdatePublicationMetadataRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<PublicationDto>($"api/publications/{publicationId}", request, ct);

    public Task<ApiResult<PublicationVersionDto>> UploadVersionAsync(
        Guid publicationId, IFormFile file, IFormFile? supplementaryFile, string? reviewerNotes, CancellationToken ct = default) =>
        PostMultipartAsync<PublicationVersionDto>(
            $"api/publications/{publicationId}/versions",
            [("file", file), ("supplementaryFile", supplementaryFile)],
            [("reviewerNotes", reviewerNotes)],
            ct);

    /// <summary>The file of one version, for anyone who can see the publication.</summary>
    public Task<(byte[] Content, string ContentType, string FileName)?> DownloadVersionAsync(
        Guid publicationId, Guid versionId, CancellationToken ct = default) =>
        GetFileAsync($"api/publications/{publicationId}/versions/{versionId}/download", ct);

    /// <summary>
    /// Admin-only: adds a version to a paper whatever step it has reached, and removes one. Both
    /// carry the reason, which the API records on the publication's history.
    /// </summary>
    public Task<ApiResult<PublicationVersionDto>> AdminUploadVersionAsync(
        Guid publicationId, IFormFile file, string comments, CancellationToken ct = default) =>
        PostMultipartAsync<PublicationVersionDto>(
            $"api/publications/{publicationId}/versions/admin",
            [("file", file)],
            [("comments", comments)],
            ct);

    public Task<ApiResult<object?>> AdminRemoveVersionAsync(
        Guid publicationId, Guid versionId, string comments, CancellationToken ct = default) =>
        DeleteJsonAsync<object?>(
            $"api/publications/{publicationId}/versions/{versionId}", new CommentsRequestDto(comments), ct);

    public Task<ApiResult<IReadOnlyList<PublicationVersionDto>>> GetVersionsAsync(Guid publicationId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PublicationVersionDto>>($"api/publications/{publicationId}/versions", ct);

    public Task<ApiResult<object?>> SubmitAsync(Guid publicationId, CancellationToken ct = default) =>
        PostAsync<object?>($"api/publications/{publicationId}/submit", ct);

    /// <summary>
    /// Admin-only: takes a published paper back out of the public catalogue. Its own call rather
    /// than a publish decision of "no": declining is the author's answer before it ever appeared,
    /// and this is an administrator removing one that is already out.
    /// </summary>
    public Task<ApiResult<object?>> UnpublishAsync(Guid publicationId, CommentsRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/publications/{publicationId}/unpublish", request, ct);

    public Task<ApiResult<object?>> PublishDecisionAsync(Guid publicationId, PublishDecisionRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/publications/{publicationId}/publish", request, ct);

    // ---------- Coordinator ----------

    public Task<ApiResult<object?>> CoordinatorFinalDecisionAsync(
        Guid publicationId, PaperReviewDecisionRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/publications/{publicationId}/coordinator-final-decision", request, ct);

    /// <summary>Every review recorded against a paper, newest first.</summary>
    public Task<ApiResult<IReadOnlyList<ReviewDto>>> GetReviewsAsync(Guid publicationId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ReviewDto>>($"api/publications/{publicationId}/reviews", ct);

    // ---------- Supervisor ----------

    /// <summary>Papers submitted by students this supervisor supervises, awaiting their review.</summary>
    public Task<ApiResult<PagedResultDto<PublicationDto>>> GetPendingForSupervisorAsync(
        int page = 1, int pageSize = Paging.AsConfigured, string? sort = null, bool descending = false,
        string? search = null, CancellationToken ct = default)
    {
        var query = $"api/publications/pending?page={Math.Max(1, page)}{Paging.SizeParam(pageSize)}";
        if (!string.IsNullOrWhiteSpace(sort)) query += $"&sortBy={Uri.EscapeDataString(sort)}";
        if (descending) query += "&sortDescending=true";
        if (!string.IsNullOrWhiteSpace(search)) query += $"&search={Uri.EscapeDataString(search.Trim())}";

        return GetAsync<PagedResultDto<PublicationDto>>(query, ct);
    }

    /// <summary>
    /// Papers a supervisor has approved that have no evaluation committee yet. This is the
    /// administrator's queue, answered in one request rather than reconstructed from the containers
    /// list, which could not see whether the supervisor had approved.
    /// </summary>
    public Task<ApiResult<IReadOnlyList<AwaitingCommitteeDto>>> GetAwaitingCommitteeAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<AwaitingCommitteeDto>>("api/publications/awaiting-committee", ct);

    public Task<ApiResult<object?>> SupervisorReviewAsync(
        Guid publicationId, PaperReviewDecisionRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/publications/{publicationId}/supervisor-review", request, ct);
}
