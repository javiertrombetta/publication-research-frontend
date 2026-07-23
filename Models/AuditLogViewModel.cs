using System.Collections.Generic;

namespace ResearchPublicationManagementSystem.Models
{
    public class AuditLogViewModel
    {
        // Summary Cards
        public int TotalLogs { get; set; }

        public int TodayLogs { get; set; }

        public int ActiveUsers { get; set; }

        public int CriticalEvents { get; set; }

        // Search / Filter
        public string? SearchTerm { get; set; }

        public string? SelectedModule { get; set; }

        public string? SelectedStatus { get; set; }

        // Audit Log List
        public List<AuditLogItemViewModel> AuditLogs { get; set; }
            = new List<AuditLogItemViewModel>();
    }
}
