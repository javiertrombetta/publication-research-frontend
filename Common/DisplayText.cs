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
        /// wherever it appears. A paper that reads Accepted in green on the listing must not read
        /// Accepted in red inside the publication, and its progress bar must not disagree with its
        /// badge. Statuses come from several backend enums (proposals, ethics, papers, containers)
        /// and are matched by name. That is safe except for "Assigned", which means a supervisor
        /// took a proposal on (resolved) but also that a committee has yet to start (in progress);
        /// the committee views colour their own status rather than asking here. Returns a colour
        /// token; <see cref="StatusBadge"/> and <see cref="StatusBar"/> dress it.
        /// </summary>
        public static string StatusColour(string? status) => status switch
        {
            // Resolved, and resolved well: nothing further is being waited on.
            // Approve is a review decision rather than a status, and reads on the same screens.
            "Accepted" or "Published" or "Verified" or "Assigned" or "Completed" or "Approve"
                => "success",

            // Resolved against the student, or sent back to them for work. Both spellings appear:
            // papers use RevisionsRequested, ethics documents RevisionRequested. Revoked is an
            // invitation an administrator withdrew, a decision against it, and the one state on
            // that screen someone might need to notice at a glance.
            "Rejected" or "DeferredToNextCycle" or "RevisionsRequested" or "RevisionRequested"
                or "Reject" or "RequestRevision" or "Revoked"
                => "danger",

            // Under way. Blue rather than the brand red, which reads as something needing
            // attention when in fact the work is simply in hand.
            "InProgress" or "In Progress" or "Pending" => "blue",

            // Started but not submitted. The student still has it, and it needs their action.
            "Draft" => "orange",

            // Nothing to do and nothing achieved: a step that turned out not to apply, one not
            // started, or an invitation that simply ran out. Grey keeps them from competing with
            // the states that carry a result.
            "NotRequired" or "NotStarted" or "Expired" => "secondary",

            // Everything still in flight.
            _ => "primary"
        };

        /// <summary>Tabler badge class for a status, a light tint of its colour.</summary>
        public static string StatusBadge(string? status) => "bg-" + StatusColour(status) + "-lt";

        /// <summary>
        /// Progress-bar class for a status, the solid form of the same colour, so a publication
        /// sitting in Draft shows an orange bar next to its orange badge rather than a red one.
        /// </summary>
        public static string StatusBar(string? status) => "rpms-stage-" + StatusColour(status);
    }
}
