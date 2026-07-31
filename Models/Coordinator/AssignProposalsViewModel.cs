using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// Submitted proposals that no supervisor has been invited to yet, and the supervisors they
    /// can be sent to.
    /// </summary>
    public class AssignProposalsViewModel
    {
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
        public List<SupervisorSelectionItem> Items { get; set; } = [];

        public bool LoadFailed { get; set; }
    }

    public class SupervisorSelectionItem
    {
        public PublicationContainerDto Container { get; set; } = null!;

        public ProposalDto Proposal { get; set; } = null!;

        public IReadOnlyList<SupervisorInvitationDto> Invitations { get; set; } = [];

        /// <summary>The supervisors who said yes — the only ones who can actually be assigned.</summary>
        public IEnumerable<SupervisorInvitationDto> Willing => Invitations.Where(i => i.IsSelected);

        public IEnumerable<SupervisorInvitationDto> AwaitingReply => Invitations.Where(i => !i.IsSelected);
    }
}
