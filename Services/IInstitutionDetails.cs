using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Services;

/// <summary>
/// The institution's own details: its name, the addresses people write to, its privacy policy.
///
/// These used to live in appsettings, which meant correcting an address was a deployment. They are
/// administrator-editable settings now, and this is what views read them through: the footer
/// appears on every page and three separate views need them, so a per-request cache keeps one page
/// from asking the API three times.
/// </summary>
public interface IInstitutionDetails
{
    /// <summary>
    /// Never throws and never returns null. A failure to reach the API falls back to the values
    /// in configuration: a footer is not worth breaking a page over, and a blank address renders
    /// as plain text rather than as a link that goes nowhere.
    /// </summary>
    Task<InstitutionSettingsDto> GetAsync(CancellationToken ct = default);

    /// <summary>
    /// Drops the cached copy, so the next read comes from the API.
    ///
    /// Needed because one of these values decides the landing page. An administrator switching the
    /// public catalogue off and being sent straight back to it would conclude the setting had not
    /// saved. A minute is a long time to watch a page contradict you.
    /// </summary>
    void Invalidate();
}
