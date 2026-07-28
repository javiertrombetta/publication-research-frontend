using System.Collections.Generic;

namespace ResearchPublicationManagementSystem.Models
{
    public class PublicationListViewModel
    {
        public SearchFilterToolbarViewModel Toolbar { get; set; } = new();

        // Statistics
        // Statistics
        public int TotalPublications { get; set; }

        public int PendingCommitteeAssignment { get; set; }

        public int UnderReview { get; set; }

        public int Approved { get; set; }

        // Table
        public List<PublicationListItemViewModel> Publications { get; set; } = new();
    }
}