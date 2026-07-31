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

    /// <summary>
    /// Where the institution publishes the authoritative policy.
    ///
    /// The fallback is the institution's home page rather than a deep link to the policy itself:
    /// this value is only reached when the API is unreachable and nobody has configured one, and
    /// a guessed deep link rots the moment the site is reorganised. The home page will still be
    /// there, and it leads to the policy. An administrator sets the exact address under System
    /// settings, which is what is used in practice.
    /// </summary>
    public string PrivacyPolicyUrl { get; set; } = "https://www.ais.ac.nz/";

    /// <summary>
    /// Where a reader writes to ask for the full text of a published paper. The catalogue shows
    /// abstracts and metadata only — the papers themselves are not downloadable from it — so this
    /// is the sole route to a copy. Empty until the address is decided, and while it is empty the
    /// catalogue says to contact the institution without offering a link that goes nowhere.
    /// </summary>
    public string ResearchEnquiriesEmail { get; set; } = string.Empty;
}
