using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;
using ResearchPublicationManagementSystem.Common;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

public class UsersApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<UserDetailDto>> GetMeAsync(CancellationToken ct = default) =>
        GetAsync<UserDetailDto>("api/users/me", ct);

    /// <summary>
    /// Says whether this person is taking work on. Not the same as an administrator enabling or
    /// disabling the account: this only governs what they are offered next, and leaves anything
    /// already assigned to them alone.
    /// </summary>
    public Task<ApiResult<object?>> SetMyAvailabilityAsync(bool isAvailable, CancellationToken ct = default) =>
        PutJsonAsync<object?>("api/users/me/availability", new SetAvailabilityRequestDto(isAvailable), ct);

    public Task<ApiResult<UserDetailDto>> UpdateMeAsync(UpdateMyProfileRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<UserDetailDto>("api/users/me", request, ct);

    public Task<ApiResult<UserDetailDto>> UploadProfilePhotoAsync(IFormFile file, CancellationToken ct = default) =>
        PostMultipartAsync<UserDetailDto>("api/users/me/photo", [("file", file)], ct: ct);

    public Task<ApiResult<UserDetailDto>> DeleteProfilePhotoAsync(CancellationToken ct = default) =>
        DeleteAsync<UserDetailDto>("api/users/me/photo", ct);

    /// <summary>Null when that user has no photo.</summary>
    public Task<(byte[] Content, string ContentType)?> GetProfilePhotoAsync(Guid userId, CancellationToken ct = default) =>
        GetBytesAsync($"api/users/{userId}/photo", ct);

    /// <summary>
    /// Enabled supervisors, for a Coordinator choosing who to send proposals to. A chooser rather
    /// than a listing, so it asks for a page big enough to hold a department's worth of them and
    /// narrows by search rather than by paging.
    /// </summary>
    public Task<ApiResult<PagedResultDto<UserListItemDto>>> GetSupervisorsAsync(
        string? search = null, int pageSize = 100, CancellationToken ct = default) =>
        GetAsync<PagedResultDto<UserListItemDto>>(
            $"api/users/supervisors?pageSize={pageSize}"
            + (string.IsNullOrWhiteSpace(search) ? "" : $"&search={Uri.EscapeDataString(search.Trim())}"), ct);

    // ---------- Administration ----------

    /// <summary>
    /// The directory, one page at a time, optionally narrowed by role, status or a search term.
    /// </summary>
    public Task<ApiResult<PagedResultDto<UserListItemDto>>> GetAllAsync(
        string? role = null, string? status = null, string? search = null,
        int page = 1, int pageSize = Paging.AsConfigured, string? sort = null, bool descending = false,
        CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["role"] = role,
            ["status"] = status,
            ["search"] = search,
            ["page"] = Math.Max(1, page).ToString(),
            ["pageSize"] = Paging.SizeValue(pageSize),
            ["sortBy"] = sort,
            ["sortDescending"] = descending ? "true" : null
        };

        var url = QueryHelpers.AddQueryString("api/users",
            parameters.Where(p => !string.IsNullOrWhiteSpace(p.Value)));

        return GetAsync<PagedResultDto<UserListItemDto>>(url, ct);
    }

    /// <summary>Records which theme this person prefers, so it follows them to another machine.</summary>
    public Task<ApiResult<object?>> SetThemeAsync(string theme, CancellationToken ct = default) =>
        PutJsonAsync<object?>("api/users/me/theme", new { theme }, ct);

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
    /// Deletes an account. The backend keeps the row and strips it rather than removing it. Every
    /// reference to a user is a Restrict foreign key, and detaching published research from its
    /// author to force a row delete would destroy the traceability this records. The reason is
    /// required and lands in the audit trail.
    /// </summary>
    public Task<ApiResult<object?>> DeleteAsync(Guid id, CommentsRequestDto request, CancellationToken ct = default) =>
        DeleteJsonAsync<object?>($"api/users/{id}", request, ct);
}
