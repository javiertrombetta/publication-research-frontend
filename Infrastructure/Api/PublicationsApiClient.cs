using Microsoft.AspNetCore.Http;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

public class PublicationsApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<PublicationDto>> GetOrCreateDraftAsync(Guid containerId, CancellationToken ct = default) =>
        PostAsync<PublicationDto>($"api/containers/{containerId}/publications", ct);

    /// <summary>404 (ApiResult.StatusCode == 404) means the paper stage isn't unlocked yet (ethics not resolved).</summary>
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

    public Task<ApiResult<IReadOnlyList<PublicationVersionDto>>> GetVersionsAsync(Guid publicationId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<PublicationVersionDto>>($"api/publications/{publicationId}/versions", ct);

    public Task<ApiResult<object?>> SubmitAsync(Guid publicationId, CancellationToken ct = default) =>
        PostAsync<object?>($"api/publications/{publicationId}/submit", ct);

    public Task<ApiResult<object?>> PublishDecisionAsync(Guid publicationId, PublishDecisionRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/publications/{publicationId}/publish", request, ct);
}
