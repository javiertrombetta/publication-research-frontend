using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// One page of a publication's trail, and the controls to move through it.
    ///
    /// The partial used to take the entries alone, which was enough while the whole trail arrived
    /// at once. It is paged now, so it needs to know how much there is and where to send somebody
    /// for the rest.
    /// </summary>
    public class ActivityHistoryViewModel
    {
        public IReadOnlyList<ActivityHistoryEntryDto> Entries { get; set; } = [];

        /// <summary>Everything recorded, not what fits on this page.</summary>
        public int TotalCount { get; set; }

        public PagerViewModel? Pager { get; set; }
    }
}
