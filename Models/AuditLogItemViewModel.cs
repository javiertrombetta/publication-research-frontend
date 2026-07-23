using System;

namespace ResearchPublicationManagementSystem.Models
{
    public class AuditLogItemViewModel
    {
        public DateTime DateTime { get; set; }

        public string User { get; set; } = string.Empty;

        public string Module { get; set; } = string.Empty;

        public string Activity { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}