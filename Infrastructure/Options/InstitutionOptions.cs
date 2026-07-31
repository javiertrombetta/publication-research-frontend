namespace ResearchPublicationManagementSystem.Infrastructure.Options;

/// <summary>
/// Details owned by the institution rather than by this application, so they can be corrected
/// without a code change.
/// </summary>
public class InstitutionOptions
{
    public const string SectionName = "Institution";

    /// <summary>
    /// Where "Contact IT" writes to. Left empty until the address is decided: an empty value
    /// renders the label as plain text, so the footer never offers a mailto: link that would
    /// silently go nowhere.
    /// </summary>
    public string ItSupportEmail { get; set; } = string.Empty;

    /// <summary>The institution's published privacy policy — the authoritative copy.</summary>
    public string PrivacyPolicyUrl { get; set; } = "https://www.ais.ac.nz/privacy-policy";

    /// <summary>
    /// Where a reader writes to ask for the full text of a published paper. The catalogue shows
    /// abstracts and metadata only — the papers themselves are not downloadable from it — so this
    /// is the sole route to a copy. Empty until the address is decided, and while it is empty the
    /// catalogue says to contact the institution without offering a link that goes nowhere.
    /// </summary>
    public string ResearchEnquiriesEmail { get; set; } = string.Empty;
}
