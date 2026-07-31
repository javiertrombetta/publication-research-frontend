using Microsoft.AspNetCore.Http;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

public class UsersApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<UserDetailDto>> GetMeAsync(CancellationToken ct = default) =>
        GetAsync<UserDetailDto>("api/users/me", ct);

    public Task<ApiResult<UserDetailDto>> UpdateMeAsync(UpdateMyProfileRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<UserDetailDto>("api/users/me", request, ct);

    public Task<ApiResult<UserDetailDto>> UploadProfilePhotoAsync(IFormFile file, CancellationToken ct = default) =>
        PostMultipartAsync<UserDetailDto>("api/users/me/photo", [("file", file)], ct: ct);

    public Task<ApiResult<UserDetailDto>> DeleteProfilePhotoAsync(CancellationToken ct = default) =>
        DeleteAsync<UserDetailDto>("api/users/me/photo", ct);

    /// <summary>Null when that user has no photo.</summary>
    public Task<(byte[] Content, string ContentType)?> GetProfilePhotoAsync(Guid userId, CancellationToken ct = default) =>
        GetBytesAsync($"api/users/{userId}/photo", ct);
}
