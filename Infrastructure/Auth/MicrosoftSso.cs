namespace ResearchPublicationManagementSystem.Infrastructure.Auth;

/// <summary>
/// The names the Microsoft sign-in is wired together with. Kept in one place so the registration in
/// Program.cs, the controller that challenges and the controller that reads the result cannot drift
/// apart, and so the redirect URI written here is the one an administrator registers with Entra.
/// </summary>
public static class MicrosoftSso
{
    /// <summary>The OpenID Connect scheme itself.</summary>
    public const string Scheme = "Microsoft";

    /// <summary>
    /// Where the result of a Microsoft sign-in lands, for the few milliseconds before it is traded
    /// for this application's own session. Separate from the session cookie deliberately: being
    /// known to Microsoft is not the same as being signed in here.
    /// </summary>
    public const string HandoverScheme = "MicrosoftHandover";

    /// <summary>
    /// The redirect URI, which has to be registered on the application in Entra exactly as it
    /// appears in the browser, host and scheme included.
    /// </summary>
    public const string CallbackPath = "/signin-microsoft";
}
