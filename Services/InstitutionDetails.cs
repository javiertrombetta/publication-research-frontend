using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;
using ResearchPublicationManagementSystem.Infrastructure.Options;

namespace ResearchPublicationManagementSystem.Services;

public class InstitutionDetails(
    SettingsApiClient settingsApi,
    IMemoryCache cache,
    IHttpContextAccessor httpContextAccessor,
    IOptions<InstitutionOptions> fallbackOptions) : IInstitutionDetails
{
    private const string CacheKeyPrefix = "institution-details";

    private readonly InstitutionOptions _fallback = fallbackOptions.Value;

    /// <summary>
    /// Two cached copies, one for visitors and one for people with an account.
    ///
    /// The API does not answer this identically to both: it withholds the IT desk's address from
    /// anyone who has not signed in, unless the institution has said to publish it. With one shared
    /// key, whoever asked first decided what everybody saw for the next minute, so a visitor's
    /// request could leave a signed-in student looking at "Contact IT" as dead grey text, and the
    /// next minute the other way round. Keyed by the only thing the two answers differ on.
    /// </summary>
    private string CacheKey =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true
            ? CacheKeyPrefix + ":signed-in"
            : CacheKeyPrefix + ":visitor";

    public async Task<InstitutionSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var key = CacheKey;

        if (cache.TryGetValue(key, out InstitutionSettingsDto? cached) && cached is not null)
        {
            return cached;
        }

        var result = await settingsApi.GetInstitutionAsync(ct);

        // Configuration is the floor, not the source of truth: it is what the site shows if the
        // API is briefly unreachable, so the footer degrades rather than disappearing.
        var details = result.Data ?? new InstitutionSettingsDto(
            "Auckland Institute of Studies", "@aisstudent.ac.nz", "@ais.ac.nz",
            _fallback.ItSupportEmail, _fallback.ResearchEnquiriesEmail, _fallback.PrivacyPolicyUrl, null,
            // Closed while the API is unreachable: offering a sign-up form that cannot be
            // submitted is worse than not offering one.
            SelfRegistrationOpen: false,
            // Off for the same reason. The catalogue's every row comes from the API, so sending a
            // visitor to it while that is down lands them on an empty page that looks like the
            // institution has published nothing; the sign-in page at least works.
            PublicCatalogueEnabled: false);

        // Short: an administrator correcting an address should see it take effect within a minute
        // rather than after a restart, and this is read on every page.
        cache.Set(key, details, TimeSpan.FromMinutes(1));
        return details;
    }

    /// <summary>Both copies, since a setting an administrator changed applies to both audiences.</summary>
    public void Invalidate()
    {
        cache.Remove(CacheKeyPrefix + ":signed-in");
        cache.Remove(CacheKeyPrefix + ":visitor");
    }
}
