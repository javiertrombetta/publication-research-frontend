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

    public Task<ApiResult<IReadOnlyList<PublicationVersionDto>>> GetVersionsAsync(Guid publicationId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PublicationVersionDto>>($"api/publications/{publicationId}/versions", ct);

    public Task<ApiResult<object?>> SubmitAsync(Guid publicationId, CancellationToken ct = default) =>
        PostAsync<object?>($"api/publications/{publicationId}/submit", ct);

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
        int page = 1, int pageSize = Paging.DefaultPageSize, CancellationToken ct = default) =>
        GetAsync<PagedResultDto<PublicationDto>>(
            $"api/publications/pending?page={Math.Max(1, page)}&pageSize={pageSize}", ct);

    /// <summary>
    /// Papers a supervisor has approved that have no evaluation committee yet — the
    /// administrator's queue, answered in one request rather than reconstructed from the
    /// containers list, which could not see whether the supervisor had approved.
    /// </summary>
    public Task<ApiResult<IReadOnlyList<AwaitingCommitteeDto>>> GetAwaitingCommitteeAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<AwaitingCommitteeDto>>("api/publications/awaiting-committee", ct);

    public Task<ApiResult<object?>> SupervisorReviewAsync(
        Guid publicationId, PaperReviewDecisionRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/publications/{publicationId}/supervisor-review", request, ct);
}
