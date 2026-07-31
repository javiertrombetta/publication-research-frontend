using System.Text.RegularExpressions;

namespace ResearchPublicationManagementSystem.Common
{
    /// <summary>
    /// Turns the backend's enum names into something readable. Statuses travel over the API as
    /// PascalCase identifiers ("InProgress", "PendingUpload"), which is right for a wire format
    /// and wrong for a badge, so views humanise them at the point of display rather than the
    /// backend inventing a second, presentational vocabulary.
    /// </summary>
    public static partial class DisplayText
    {
        /// <summary>
        /// "InProgress" -> "In Progress", "PendingUpload" -> "Pending Upload".
        /// Acronyms stay intact ("PDFUpload" -> "PDF Upload"), and text that already has
        /// spaces is left alone.
        /// </summary>
        public static string Humanise(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            if (value.Contains(' ')) return value;

            // Only one alternative matches at a time, so the groups from the other are empty.
            return PascalCaseBoundary().Replace(value, "$1$3 $2$4");
        }

        // Split where a lower-case letter or digit meets an upper-case one, and where a run of
        // upper-case letters is followed by a word (the last capital starts the next word).
        [GeneratedRegex(@"([a-z0-9])([A-Z])|([A-Z]+)([A-Z][a-z])")]
        private static partial Regex PascalCaseBoundary();

        /// <summary>
        /// The one place a status is turned into a colour, so the same status is the same colour
        /// wherever it appears — a paper that reads Accepted in green on the listing must not read
        /// Accepted in red inside the publication, and its progress bar must not disagree with its
        /// badge. Statuses come from several backend enums (proposals, ethics, papers, containers)
        /// and are matched by name, which is safe because the names don't collide across them.
        /// Returns a colour token; <see cref="StatusBadge"/> and <see cref="StatusBar"/> dress it.
        /// </summary>
        public static string StatusColour(string? status) => status switch
        {
            // Resolved, and resolved well: nothing further is being waited on.
            "Accepted" or "Published" or "Verified" or "Assigned" or "Completed"
                => "success",

            // Resolved against the student, or sent back to them for work. Both spellings
            // appear: papers use RevisionsRequested, ethics documents RevisionRequested.
            "Rejected" or "DeferredToNextCycle" or "RevisionsRequested" or "RevisionRequested"
                => "danger",

            // Under way. Blue rather than the brand red, which reads as something needing
            // attention when in fact the work is simply in hand.
            "InProgress" or "In Progress" => "blue",

            // Started but not submitted — the student still has it, and it needs their action.
            "Draft" => "orange",

            // Nothing to do and nothing achieved: a step that turned out not to apply, or one
            // not started. Grey keeps them from competing with the states that carry a result.
            "NotRequired" or "NotStarted" => "secondary",

            // Everything still in flight.
            _ => "primary"
        };

        /// <summary>Tabler badge class for a status — a light tint of its colour.</summary>
        public static string StatusBadge(string? status) => "bg-" + StatusColour(status) + "-lt";

        /// <summary>
        /// Progress-bar class for a status — the solid form of the same colour, so a publication
        /// sitting in Draft shows an orange bar next to its orange badge rather than a red one.
        /// </summary>
        public static string StatusBar(string? status) => "rpms-stage-" + StatusColour(status);
    }
}
