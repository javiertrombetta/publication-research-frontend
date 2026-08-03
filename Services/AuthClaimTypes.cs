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

    /// <summary>
    /// The order this person has put their sidebar in, as routes separated by spaces. Kept on the
    /// principal because the menu is drawn on every page and asking the API each time for
    /// something this small would be a request per page.
    ///
    /// On the principal rather than in the browser's own storage, which is where it started: a
    /// browser is shared, and one person's arrangement was being handed to whoever signed in next.
    /// </summary>
    public const string SidebarOrder = "sidebar_order";
}
