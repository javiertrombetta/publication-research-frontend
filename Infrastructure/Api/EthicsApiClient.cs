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

    public Task<ApiResult<EthicsDocumentDto>> UploadDocumentAsync(Guid containerId, string documentType, IFormFile file, CancellationToken ct = default) =>
        PostMultipartAsync<EthicsDocumentDto>(
            $"api/containers/{containerId}/ethics/documents",
            [("file", file)],
            [("documentType", documentType)],
            ct);

    public Task<ApiResult<IReadOnlyList<EthicsDocumentDto>>> GetDocumentsAsync(Guid containerId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<EthicsDocumentDto>>($"api/containers/{containerId}/ethics/documents", ct);
}
