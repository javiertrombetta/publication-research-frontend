using Microsoft.AspNetCore.Http;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

public class EthicsApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<EthicsGuidanceDto>> GetGuidanceAsync(CancellationToken ct = default) =>
        GetAsync<EthicsGuidanceDto>("api/ethics/guidance", ct);

    public Task<ApiResult<EthicsDeclarationDto>> SubmitDeclarationAsync(Guid containerId, string response, CancellationToken ct = default) =>
        PostJsonAsync<EthicsDeclarationDto>($"api/containers/{containerId}/ethics/declaration", new EthicsDeclarationRequestDto(response), ct);

    public Task<ApiResult<EthicsApprovalDto>> GetApprovalAsync(Guid containerId, CancellationToken ct = default) =>
        GetAsync<EthicsApprovalDto>($"api/containers/{containerId}/ethics", ct);

    /// <summary>
    /// What this publication has been asked to supply, and what is still outstanding. Read from
    /// the publication's own snapshot, so it is the list this student was given rather than the
    /// one a student starting today would get.
    /// </summary>
    public Task<ApiResult<IReadOnlyList<RequiredEthicsDocumentDto>>> GetRequiredDocumentsAsync(
        Guid containerId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<RequiredEthicsDocumentDto>>($"api/containers/{containerId}/ethics/required-documents", ct);

    /// <summary>
    /// <paramref name="documentType"/> is the requirement's id. It used to be an enum name; the
    /// set of documents is configurable now, so identity travels rather than a fixed word.
    /// </summary>
    public Task<ApiResult<EthicsDocumentDto>> UploadDocumentAsync(Guid containerId, string documentType, IFormFile file, CancellationToken ct = default) =>
        PostMultipartAsync<EthicsDocumentDto>(
            $"api/containers/{containerId}/ethics/documents",
            [("file", file)],
            [("documentType", documentType)],
            ct);

    /// <summary>One uploaded ethics document, so a reviewer can read what they are approving.</summary>

    public Task<(byte[] Content, string ContentType, string FileName)?> DownloadDocumentAsync(

        Guid containerId, Guid documentId, CancellationToken ct = default) =>

        GetFileAsync($"api/containers/{containerId}/ethics/documents/{documentId}/download", ct);


    public Task<ApiResult<IReadOnlyList<EthicsDocumentDto>>> GetDocumentsAsync(Guid containerId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<EthicsDocumentDto>>($"api/containers/{containerId}/ethics/documents", ct);

    // ---------- Coordinator ----------

    /// <summary>Confirms, or overrides, a Supervisor's finding that no ethics documentation is needed.</summary>
    public Task<ApiResult<object?>> CoordinatorNotRequiredReviewAsync(
        Guid containerId, CoordinatorNotRequiredReviewRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/containers/{containerId}/ethics/coordinator-not-required-review", request, ct);

    public Task<ApiResult<object?>> CoordinatorDocumentReviewAsync(
        Guid containerId, CoordinatorDocumentReviewRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/containers/{containerId}/ethics/coordinator-document-review", request, ct);

    /// <summary>Closes the ethics stage after the Head of Department has had their say.</summary>
    public Task<ApiResult<object?>> CoordinatorFinalDecisionAsync(
        Guid containerId, CoordinatorFinalDecisionRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/containers/{containerId}/ethics/coordinator-final-decision", request, ct);

    // ---------- Supervisor ----------

    /// <summary>Whether this research needs ethics approval documentation at all.</summary>
    public Task<ApiResult<object?>> SupervisorDecisionAsync(
        Guid containerId, SupervisorRequirementDecisionRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/containers/{containerId}/ethics/supervisor-decision", request, ct);

    /// <summary>Accepts the uploaded ethics documents, or sends them back for revision.</summary>
    public Task<ApiResult<object?>> SupervisorReviewAsync(
        Guid containerId, DocumentReviewDecisionRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/containers/{containerId}/ethics/supervisor-review", request, ct);

    // ---------- Head of Department ----------

    /// <summary>
    /// The Head of Department's comments on a student's ethics documentation. This is a review
    /// rather than a decision. The coordinator closes the stage afterwards.
    /// </summary>
    public Task<ApiResult<object?>> HeadOfDepartmentReviewAsync(
        Guid containerId, HeadOfDepartmentReviewRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/containers/{containerId}/ethics/hod-review", request, ct);
}
