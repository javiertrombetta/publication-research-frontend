using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// A committee member's assignments. Their whole job is one decision per paper, so the
    /// dashboard is split by whether that decision has been made.
    /// </summary>
    public class CommitteeDashboardViewModel
    {

        /// <summary>
        /// Which page of the queue this is. Null where everything fits on one, so the controls
        /// only appear when there is somewhere to go.
        /// </summary>
        public PagerViewModel? Pager { get; set; }
        public List<CommitteeAssignmentItem> Items { get; set; } = [];

        public bool LoadFailed { get; set; }

        public IReadOnlyList<CommitteeAssignmentItem> AwaitingMe =>
            Items.Where(i => !i.HasDecided).ToList();

        public IReadOnlyList<CommitteeAssignmentItem> Decided =>
            Items.Where(i => i.HasDecided).ToList();
    }

    /// <summary>One paper this member has been asked to evaluate.</summary>
    public class CommitteeAssignmentItem
    {
        public CommitteeDto Committee { get; set; } = null!;

        /// <summary>Null if the paper could not be read. The row is still worth showing.</summary>
        public CommitteePaperDto? Paper => Committee.Paper;

        /// <summary>This member's own place on the committee.</summary>
        public CommitteeMemberDto? Me { get; set; }

        public bool HasDecided => Me?.HasDecided == true;

        public string Title => Paper?.Title is { Length: > 0 } title ? title : "Untitled paper";
    }
}
