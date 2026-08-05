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
            || !string.IsNullOrWhiteSpace(HistoryAction) || HistoryActor is not null;
    }
}
