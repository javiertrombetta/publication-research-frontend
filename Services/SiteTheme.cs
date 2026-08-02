using Microsoft.AspNetCore.Http;

namespace ResearchPublicationManagementSystem.Services;

/// <summary>
/// Whether this visitor wants the site light or dark, and where that answer is kept.
///
/// A cookie rather than the session, for two reasons. It has to survive signing out and signing
/// back in, which a session tied to the sign-in does not; and it has to be readable on the server
/// before the page is written, so the correct theme is in the first byte of HTML. Deciding it in
/// JavaScript after the page has loaded is what produces the white flash on every navigation that
/// a dark theme is judged by.
///
/// The account carries it too, which is what makes it the person's preference rather than the
/// machine's: this cookie is refilled from the account at sign-in.
/// </summary>
public static class SiteTheme
{
    public const string Light = "light";
    public const string Dark = "dark";

    private const string CookieName = "rpms-theme";

    /// <summary>
    /// A year. Long enough that nobody meets the question twice, and it holds nothing private:
    /// the value is the word "light" or the word "dark".
    /// </summary>
    private static readonly CookieOptions Options = new()
    {
        MaxAge = TimeSpan.FromDays(365),
        HttpOnly = false,
        IsEssential = true,
        SameSite = SameSiteMode.Lax,
        Path = "/"
    };

    /// <summary>
    /// What to draw. Light where nothing has been chosen: a value has to be written into the
    /// document, and guessing dark for somebody who has never said is the more surprising of the
    /// two. The switch itself is what expresses a preference.
    /// </summary>
    public static string For(HttpContext context) =>
        context.Request.Cookies[CookieName] == Dark ? Dark : Light;

    public static bool IsDark(HttpContext context) => For(context) == Dark;

    /// <summary>
    /// Writes the choice, ignoring anything that is not one of the two themes, and hands back what
    /// was actually written.
    ///
    /// It returns the value because reading it back with <see cref="For"/> would not work: this
    /// writes to the response, and that reads the request, which still holds whatever the browser
    /// sent. Asking for it again in the same request gives the previous answer, which is how the
    /// account came to be saved with the theme somebody had just switched away from.
    /// </summary>
    public static string Set(HttpContext context, string? theme)
    {
        var chosen = theme == Dark ? Dark : Light;
        context.Response.Cookies.Append(CookieName, chosen, Options);
        return chosen;
    }

    /// <summary>The other one, which is what the switch offers.</summary>
    public static string Opposite(string theme) => theme == Dark ? Light : Dark;
}
