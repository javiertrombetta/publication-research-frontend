using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ResearchPublicationManagementSystem.Common
{
    /// <summary>
    /// The attributes a button that records a decision carries.
    ///
    /// Two things are true of every one of them. It asks before it acts, because these are the
    /// clicks nobody wants to make by mistake: sending a student's work back, closing a stage,
    /// appointing a committee. And it may or may not insist on a comment, which is this
    /// institution's choice and can be changed in System settings, so no screen can decide it.
    ///
    /// Written here rather than typed onto thirty buttons, so that a screen added later gets the
    /// same behaviour by naming its decision instead of by remembering three attributes.
    /// </summary>
    public static class DecisionButton
    {
        /// <param name="commentRequired">What IDecisionComments said about this decision.</param>
        /// <param name="commentFieldId">The id of the field holding the reason. Ignored when a comment is not required.</param>
        /// <param name="confirm">The question asked before the decision goes through.</param>
        /// <param name="requiredMessage">What to say when the reason is missing. A general one is used if none is given.</param>
        public static IHtmlContent Attributes(
            bool commentRequired, string? commentFieldId, string confirm, string? requiredMessage = null)
        {
            var builder = new HtmlContentBuilder();

            builder.AppendHtml("data-rpms-confirm=\"")
                   .Append(confirm)
                   .AppendHtml("\"");

            if (!commentRequired || string.IsNullOrWhiteSpace(commentFieldId))
            {
                return builder;
            }

            builder.AppendHtml(" data-rpms-needs-comments=\"")
                   .Append(commentFieldId)
                   .AppendHtml("\" data-rpms-needs-comments-message=\"")
                   .Append(requiredMessage ?? "This institution asks for a comment on this decision.")
                   .AppendHtml("\"");

            return builder;
        }

        /// <summary>
        /// What to put under a comment box, so the field says whether it has to be filled in
        /// before somebody finds out by pressing a button.
        /// </summary>
        public static string Hint(bool required) => required
            ? "Required for this decision."
            : "Optional. Recorded on the publication's history.";
    }
}
