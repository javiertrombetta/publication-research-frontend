using System.Security.Cryptography;

namespace ResearchPublicationManagementSystem.Common;

/// <summary>
/// One unguessable value per response, naming the scripts this page actually ships with.
///
/// The policy used to say 'unsafe-inline', which permits any inline script the page carries,
/// including one that arrived in it by accident. That is the whole of what a content security
/// policy is for, so saying it gave most of the protection away: the other rules stopped a script
/// being fetched from somewhere else, and left the more likely case, a script written into the
/// page itself, entirely allowed.
///
/// With a nonce the browser runs the twenty blocks this site wrote and refuses everything else,
/// because everything else is missing a number it cannot guess. New every response, so it cannot
/// be learned from one page and reused on the next.
///
/// Only script. Inline style stays permitted: a nonce covers a style block and not a style
/// attribute, and the views set style attributes throughout. Injected CSS can do far less than
/// injected script, so that is the trade worth taking rather than rewriting every attribute.
/// </summary>
public static class ScriptNonce
{
    private const string Key = "rpms-script-nonce";

    /// <summary>Makes this response's nonce. Called once, by the middleware that writes the policy.</summary>
    public static string Issue(HttpContext context)
    {
        // Hex rather than base64. Both are fine by the specification, but base64 can contain a
        // plus sign, which Razor writes into the attribute as an entity: the browser decodes it and
        // matches, and anybody comparing the header against the markup by eye concludes it does
        // not. An alphabet with nothing to encode spares them the doubt.
        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        context.Items[Key] = nonce;
        return nonce;
    }

    /// <summary>
    /// This response's nonce, for a view putting it on a script tag. Empty on a response that never
    /// went through the middleware, which leaves the tag without the attribute rather than with a
    /// wrong one: a missing nonce is a script the browser refuses, which is visible, and a wrong
    /// one is the same thing spelled less clearly.
    /// </summary>
    public static string ScriptNonceValue(this HttpContext? context) =>
        context?.Items.TryGetValue(Key, out var value) == true && value is string nonce ? nonce : string.Empty;
}
