using System.Collections.Generic;

namespace ResearchPublicationManagementSystem.Models
{
    public class ProposalListViewModel
    {
        public SearchFilterToolbarViewModel Toolbar { get; set; } = new();

        // Statistics
        public int TotalProposals { get; set; }

        public int PendingAssignment { get; set; }

        public int UnderReview { get; set; }

        public int Approved { get; set; }

        // Table
        public List<ProposalListItemViewModel> Proposals { get; set; } = new();
    }
}
