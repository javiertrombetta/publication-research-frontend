using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>Everything belonging to ONE publication: its proposals, ethics workflow and paper.</summary>
    public class PublicationDetailViewModel
    {
        public PublicationContainerDto Container { get; set; } = null!;

        public IReadOnlyList<ProposalDto> Proposals { get; set; } = [];

        public EthicsApprovalDto? EthicsApproval { get; set; }

        public PublicationDto? Publication { get; set; }

        /// <summary>
        /// Every recorded action on this publication, newest first, with the comment each actor
        /// left. Shown as its own tab so the student can follow the whole chronology.
        /// </summary>
        public IReadOnlyList<ActivityHistoryEntryDto> History { get; set; } = [];

        /// <summary>
        /// How much of the trail there is, and which page of it is shown. A publication that has
        /// been through three stages, several revisions and a committee accumulates a long one.
        /// </summary>
        public int HistoryTotal { get; set; }
        public PagerViewModel? HistoryPager { get; set; }

        /// <summary>
        /// Which tab to open on. The tabs are switched in the browser, so a page turned in the
        /// trail is a fresh request that would otherwise come back on the first tab, and the
        /// reader would be put back at the top of a screen they had already left.
        /// </summary>
        public string ActiveTab { get; set; } = "progress";

        public bool HistoryIsOpen => ActiveTab == "history";

        /// <summary>
        /// The ethics documents this publication was asked for, and which of them have arrived.
        /// Only read once the supervisor has asked for any; empty otherwise.
        /// </summary>
        public IReadOnlyList<RequiredEthicsDocumentDto> RequiredEthicsDocuments { get; set; } = [];

        /// <summary>
        /// What was actually uploaded, and every version of the paper. Filled in where a screen
        /// shows the whole of a publication rather than the part its reader is working on.
        /// </summary>
        public IReadOnlyList<EthicsDocumentDto> EthicsDocuments { get; set; } = [];

        public IReadOnlyList<PublicationVersionDto> PaperVersions { get; set; } = [];

        /// <summary>
        /// What the evaluation committee said, where one has been appointed. Read by the screens
        /// that show a whole publication to somebody overseeing it rather than working on it.
        /// </summary>
        public IReadOnlyList<ReviewDto> Reviews { get; set; } = [];

        /// <summary>
        /// Every ethics document this institution asks for, so an administrator adding one can
        /// say which it is. Only filled in on the screen that offers that.
        /// </summary>
        public IReadOnlyList<EthicsDocumentRequirementDto> EthicsRequirements { get; set; } = [];

        /// <summary>
        /// Whether the proposals have left the student's hands. A draft or a rejected proposal is
        /// still theirs to work on; anything else has been submitted and is locked, which is the
        /// same rule Create_proposals applies to its own form.
        /// </summary>
        public bool ProposalsAreSubmitted =>
            Proposals.Any(p => p.Status is not (ProposalStatus.Draft or ProposalStatus.Rejected));

        /// <summary>
        /// Whether every document asked for has been uploaded. False when nothing has been asked
        /// for yet, since there is nothing to have finished.
        /// </summary>
        public bool EthicsDocumentsAreComplete =>
            RequiredEthicsDocuments.Count > 0 && RequiredEthicsDocuments.All(d => d.IsSatisfied);

        public string DisplayTitle => Container.DisplayTitle;

        // ---------- Searching the history ----------

        /// <summary>What this publication's own trail can be filtered by.</summary>
        public IReadOnlyList<string> HistoryActions { get; set; } = [];

        public IReadOnlyList<Infrastructure.Api.Dto.ActivityHistoryActorDto> HistoryActors { get; set; } = [];

        public DateOnly? HistoryFrom { get; set; }

        public DateOnly? HistoryTo { get; set; }

        public string? HistoryAction { get; set; }

        public Guid? HistoryActor { get; set; }

        /// <summary>Whether anything is narrowing the trail, so the screen can offer a way back.</summary>
        public bool HistoryIsFiltered =>
            HistoryFrom is not null || HistoryTo is not null
            || !string.IsNullOrWhiteSpace(HistoryAction) || HistoryActor is not null
            || !string.IsNullOrWhiteSpace(HistorySearch);

        // ---------- What the reader has asked of this record ----------

        /// <summary>
        /// Which controller is drawing this. The record itself is one shared view read by a
        /// coordinator and by a Head of Department, and every link it writes has to come back to
        /// whichever of them the reader arrived through.
        /// </summary>
        public string Controller { get; set; } = "Coordinator";

        /// <summary>
        /// Which screen the reader arrived from, so the way back returns there. Three of the
        /// administrator's screens open the same record, and a back link that always went to one
        /// of them sent two thirds of its readers somewhere they had not been.
        /// </summary>
        public string? CameFrom { get; set; }

        /// <summary>Free text over the trail: what happened, who did it, what they wrote.</summary>
        public string? HistorySearch { get; set; }

        public string? HistorySort { get; set; }
        public bool HistoryDescending { get; set; }

        /// <summary>
        /// A heading that orders one of this record's listings.
        ///
        /// Every listing here names its own query keys. A record shows five at once, and a single
        /// pair of "sort" and "desc" would mean ordering the proposals reordered the trail as
        /// well. The tab travels with them, so ordering something does not put the reader back on
        /// the tab they came from.
        /// </summary>
        public SortableColumnViewModel SortColumn(
            string listing, string column, string label, string? currentSort, bool currentDescending,
            bool descendingFirst = false)
        {
            var route = new Dictionary<string, string?>
            {
                ["id"] = Container.Id.ToString(),
                ["tab"] = ActiveTab
            };

            if (!string.IsNullOrWhiteSpace(HistorySearch)) route["historySearch"] = HistorySearch;
            if (!string.IsNullOrWhiteSpace(CameFrom)) route["from"] = CameFrom;

            // Everything the reader has already asked of the other listings, carried along, or
            // ordering one of them would put the rest back to their defaults.
            foreach (var (key, value, descending) in new[]
            {
                ("history", HistorySort, HistoryDescending),
                ("proposals", ProposalsSort, ProposalsDescending),
                ("documents", DocumentsSort, DocumentsDescending),
                ("versions", VersionsSort, VersionsDescending),
                ("reviews", ReviewsSort, ReviewsDescending)
            })
            {
                if (key == listing || string.IsNullOrWhiteSpace(value)) continue;
                route[key + "Sort"] = value;
                route[key + "Desc"] = descending.ToString().ToLowerInvariant();
            }

            return new SortableColumnViewModel
            {
                Controller = Controller,
                Action = "Publication",
                Column = column,
                Label = label,
                CurrentSort = currentSort,
                CurrentDescending = currentDescending,
                DescendingFirst = descendingFirst,
                RouteValues = route,
                SortKey = listing + "Sort",
                DescendingKey = listing + "Desc"
            };
        }

        public SortableColumnViewModel HistoryColumn(string column, string label, bool descendingFirst = false) =>
            SortColumn("history", column, label, HistorySort, HistoryDescending, descendingFirst);

        /// <summary>
        /// The four listings on the Contents tab. Each is returned whole rather than a page at a
        /// time, because each is bounded by the process rather than by the database: a round holds
        /// three proposals, a committee has three seats, the ethics stage asks for a fixed set. A
        /// pager on them would never show a second page. They are still worth ordering.
        /// </summary>
        public string? ProposalsSort { get; set; }
        public bool ProposalsDescending { get; set; }

        public string? DocumentsSort { get; set; }
        public bool DocumentsDescending { get; set; }

        public string? VersionsSort { get; set; }
        public bool VersionsDescending { get; set; }

        public string? ReviewsSort { get; set; }
        public bool ReviewsDescending { get; set; }

        public SortableColumnViewModel ProposalsColumn(string column, string label, bool descendingFirst = false) =>
            SortColumn("proposals", column, label, ProposalsSort, ProposalsDescending, descendingFirst);

        public SortableColumnViewModel DocumentsColumn(string column, string label, bool descendingFirst = false) =>
            SortColumn("documents", column, label, DocumentsSort, DocumentsDescending, descendingFirst);

        public SortableColumnViewModel VersionsColumn(string column, string label, bool descendingFirst = false) =>
            SortColumn("versions", column, label, VersionsSort, VersionsDescending, descendingFirst);

        public SortableColumnViewModel ReviewsColumn(string column, string label, bool descendingFirst = false) =>
            SortColumn("reviews", column, label, ReviewsSort, ReviewsDescending, descendingFirst);
    }
}
