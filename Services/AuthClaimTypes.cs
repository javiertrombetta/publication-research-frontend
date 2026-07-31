namespace ResearchPublicationManagementSystem.Services;

/// <summary>Custom claim type names used on the cookie-auth ClaimsPrincipal to carry backend JWT tokens.</summary>
public static class AuthClaimTypes
{
    public const string AccessToken = "access_token";
    public const string RefreshToken = "refresh_token";
    public const string AccessTokenExpiresAt = "access_token_expires_at";

    /// <summary>
    /// Whether the user has a profile photo. Kept on the principal so the navbar avatar can
    /// decide between the photo and the initials without an API call on every page render.
    /// </summary>
    public const string HasProfilePhoto = "has_profile_photo";
}
