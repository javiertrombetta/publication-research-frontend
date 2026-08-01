using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// Every coordinator's saved sets of supervisors, for an administrator to tidy up.
    ///
    /// Personal lists accumulate. A coordinator who leaves takes none of theirs with them, and a
    /// group naming three people who no longer supervise is worse than no group at all, because it
    /// looks like a shortcut and quietly ticks nobody.
    /// </summary>
    public class SupervisorGroupsViewModel
    {
        public IReadOnlyList<SupervisorGroupDto> Groups { get; set; } = [];

        /// <summary>
        /// Every supervisor account, so membership can be changed here. Not only the available
        /// ones: an administrator editing a group is looking at a list kept over months, and
        /// hiding somebody who is away this week would drop them from it on save.
        /// </summary>
        public IReadOnlyList<UserListItemDto> Supervisors { get; set; } = [];

        /// <summary>Matches a group's name, its owner's name or any member's name.</summary>
        public string? Search { get; set; }

        public bool LoadFailed { get; set; }

        public bool HasSearch => !string.IsNullOrWhiteSpace(Search);

        /// <summary>
        /// Groups that would tick nobody: every member either disabled or unavailable. The first
        /// thing an administrator on this screen is looking for.
        /// </summary>
        public int EmptyInPractice => Groups.Count(g => g.AvailableCount == 0);

        /// <summary>How many coordinators keep groups at all.</summary>
        public int OwnerCount => Groups.Select(g => g.OwnerId).Distinct().Count();
    }
}
