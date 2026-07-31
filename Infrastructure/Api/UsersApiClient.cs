using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
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

    /// <summary>Enabled supervisors, for a Coordinator choosing who to send proposals to.</summary>
    public Task<ApiResult<IReadOnlyList<UserListItemDto>>> GetSupervisorsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<UserListItemDto>>("api/users/supervisors", ct);

    // ---------- Administration ----------

    /// <summary>Every account, optionally narrowed by role, status or a search term.</summary>
    public Task<ApiResult<IReadOnlyList<UserListItemDto>>> GetAllAsync(
        string? role = null, string? status = null, string? search = null, CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["role"] = role,
            ["status"] = status,
            ["search"] = search
        };

        var url = QueryHelpers.AddQueryString("api/users",
            parameters.Where(p => !string.IsNullOrWhiteSpace(p.Value)));

        return GetAsync<IReadOnlyList<UserListItemDto>>(url, ct);
    }

    public Task<ApiResult<UserDetailDto>> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<UserDetailDto>($"api/users/{id}", ct);

    public Task<ApiResult<UserDetailDto>> UpdateAsync(Guid id, UpdateUserRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<UserDetailDto>($"api/users/{id}", request, ct);

    /// <summary>Grants an operational role, replacing whatever the account had.</summary>
    public Task<ApiResult<object?>> ChangeRoleAsync(Guid id, ChangeUserRoleRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<object?>($"api/users/{id}/role", request, ct);

    public Task<ApiResult<object?>> EnableAsync(Guid id, CommentsRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<object?>($"api/users/{id}/enable", request, ct);

    public Task<ApiResult<object?>> DisableAsync(Guid id, CommentsRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<object?>($"api/users/{id}/disable", request, ct);

    /// <summary>Sends the account a password-reset email; it does not set a password here.</summary>
    public Task<ApiResult<object?>> ResetPasswordAsync(Guid id, CommentsRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>($"api/users/{id}/reset-password", request, ct);

    /// <summary>Creates an account directly. The backend marks it verified and enabled.</summary>
    public Task<ApiResult<UserDetailDto>> CreateAsync(CreateUserRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<UserDetailDto>("api/users", request, ct);

    /// <summary>
    /// Deletes an account. The backend keeps the row and strips it rather than removing it —
    /// every reference to a user is a Restrict foreign key, and detaching published research
    /// from its author to force a row delete would destroy the traceability this records.
    /// The reason is required and lands in the audit trail.
    /// </summary>
    public Task<ApiResult<object?>> DeleteAsync(Guid id, CommentsRequestDto request, CancellationToken ct = default) =>
        DeleteJsonAsync<object?>($"api/users/{id}", request, ct);
}
