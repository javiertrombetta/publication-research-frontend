using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// Submitted proposals that no supervisor has been invited to yet, and the supervisors they
    /// can be sent to.
    /// </summary>
    public class AssignProposalsViewModel
    {

        /// <summary>
        /// Which page of the queue this is. Null where everything fits on one, so the controls
        /// only appear when there is somewhere to go.
        /// </summary>
        public PagerViewModel? Pager { get; set; }
        public IReadOnlyList<ProposalDto> Proposals { get; set; } = [];

        public IReadOnlyList<UserListItemDto> Supervisors { get; set; } = [];

        /// <summary>
        /// The coordinator's publications, used only to put a student's name against a proposal —
        /// a ProposalDto carries its container's id and nothing about who wrote it.
        /// </summary>
        public IReadOnlyList<PublicationContainerDto> Containers { get; set; } = [];

        public bool LoadFailed { get; set; }

        public string StudentFor(Guid containerId) =>
            Containers.FirstOrDefault(c => c.Id == containerId)?.StudentName ?? "Unknown student";

        /// <summary>Proposals grouped by student, since they are sent out per student.</summary>
        public IEnumerable<IGrouping<Guid, ProposalDto>> ByPublication =>
            Proposals.GroupBy(p => p.PublicationContainerId);
    }

    /// <summary>Proposals a supervisor has offered to take on, awaiting the coordinator.</summary>
    public class SupervisorSelectionsViewModel
    {

        /// <summary>
        /// Which page of the queue this is. Null where everything fits on one, so the controls
        /// only appear when there is somewhere to go.
        /// </summary>
        public PagerViewModel? Pager { get; set; }
        public List<SupervisorSelectionItem> Items { get; set; } = [];

        public bool LoadFailed { get; set; }
    }

    public class SupervisorSelectionItem
    {
        /// <summary>Carried on the proposal itself, so the screen needs no second request to name the author.</summary>
        public string StudentName { get; set; } = string.Empty;

        public ProposalDto Proposal { get; set; } = null!;

        public IReadOnlyList<SupervisorInvitationDto> Invitations { get; set; } = [];

        /// <summary>The supervisors who said yes — the only ones who can actually be assigned.</summary>
        public IEnumerable<SupervisorInvitationDto> Willing => Invitations.Where(i => i.IsSelected);

        public IEnumerable<SupervisorInvitationDto> AwaitingReply => Invitations.Where(i => !i.IsSelected);
    }
}
