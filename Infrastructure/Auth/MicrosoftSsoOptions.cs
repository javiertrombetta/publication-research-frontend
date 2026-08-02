namespace ResearchPublicationManagementSystem.Infrastructure.Auth;

/// <summary>
/// What this deployment needs in order to sign people in with their institutional Microsoft
/// account. Every value comes from the deployment, and there is nothing sensible to default: a
/// tenant and an application belong to the institution, not to this repository.
///
/// Nothing here is switched on until all of it is present. Absent, the site behaves exactly as it
/// does today: passwords only, no button, no redirect, no scheme registered.
/// </summary>
public sealed class MicrosoftSsoOptions
{
    public const string SectionName = "AzureAd";

    /// <summary>Where Entra lives. Only worth changing for a sovereign cloud.</summary>
    public string Instance { get; set; } = "https://login.microsoftonline.com/";

    /// <summary>The AIS directory the sign-in happens against.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>This site's own app registration.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Its secret. Held as a container app secret, never in a file in this repository.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// The scope that gets a token the API will accept, of the form
    /// <c>api://{api-app-registration}/access_as_user</c>. Without it the site receives a token for
    /// itself, which the API is right to refuse.
    /// </summary>
    public string ApiScope { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is set up at all. Any missing piece means no: half-configured sign-on that
    /// fails at the identity provider is worse than a button that was never shown.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TenantId)
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret)
        && !string.IsNullOrWhiteSpace(ApiScope);

    public string Authority => $"{Instance.TrimEnd('/')}/{TenantId}/v2.0";
}
