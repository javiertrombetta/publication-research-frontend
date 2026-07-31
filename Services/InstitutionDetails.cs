using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;
using ResearchPublicationManagementSystem.Infrastructure.Options;

namespace ResearchPublicationManagementSystem.Services;

public class InstitutionDetails(
    SettingsApiClient settingsApi,
    IMemoryCache cache,
    IOptions<InstitutionOptions> fallbackOptions) : IInstitutionDetails
{
    private const string CacheKey = "institution-details";

    private readonly InstitutionOptions _fallback = fallbackOptions.Value;

    public async Task<InstitutionSettingsDto> GetAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out InstitutionSettingsDto? cached) && cached is not null)
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
        cache.Set(CacheKey, details, TimeSpan.FromMinutes(1));
        return details;
    }

    public void Invalidate() => cache.Remove(CacheKey);
}
