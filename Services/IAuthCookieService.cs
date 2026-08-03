using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Services;

public interface IAuthCookieService
{
    Task SignInAsync(HttpContext httpContext, AuthResponseDto auth, bool isPersistent = true);

    Task SignOutAsync(HttpContext httpContext);

    /// <summary>
    /// Attempts one silent token refresh using the given refresh token, re-issuing the auth
    /// cookie with the rotated tokens on success. Returns the new access token, or null if the
    /// refresh token was itself invalid/expired (caller should sign the user out).
    /// </summary>
    Task<string?> TryRefreshAsync(HttpContext httpContext, string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Re-issues the auth cookie with the profile-photo flag updated, so the navbar avatar
    /// reflects an upload or removal immediately instead of only after the next sign-in.
    /// </summary>
    Task SetProfilePhotoFlagAsync(HttpContext httpContext, bool hasPhoto);

    /// <summary>
    /// Re-issues the auth cookie with this person's sidebar order updated, so the menu keeps the
    /// arrangement they have just made on every page from here rather than from the next sign-in.
    /// </summary>
    Task SetSidebarOrderAsync(HttpContext httpContext, string order);
}
