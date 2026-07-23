using System.ComponentModel.DataAnnotations;

namespace ResearchPublicationManagementSystem.Models
{
    public class SystemSettingsViewModel
    {
        // General Settings
        [Display(Name = "System Name")]
        public string SystemName { get; set; } = "";

        [Display(Name = "Institution Name")]
        public string InstitutionName { get; set; } = "";

        [Display(Name = "Academic Year")]
        public string AcademicYear { get; set; } = "";

        // Proposal Settings
        [Display(Name = "Maximum Proposal Submission")]
        public int MaximumProposalSubmission { get; set; }

        [Display(Name = "Supervisor Preference Limit")]
        public int SupervisorPreferenceLimit { get; set; }

        [Display(Name = "Allow Proposal Revisions")]
        public bool AllowProposalRevisions { get; set; }

        // Ethics Settings
        [Display(Name = "Enable Ethics Approval Module")]
        public bool EnableEthicsApproval { get; set; }

        [Display(Name = "Required Ethics Forms")]
        public int RequiredEthicsForms { get; set; }

        // Upload Settings
        [Display(Name = "Maximum Upload File Size (MB)")]
        public int MaximumUploadFileSize { get; set; }

        [Display(Name = "Allowed File Types")]
        public string AllowedFileTypes { get; set; } = "";

        [Display(Name = "Notify Student")]
        public bool NotifyStudent { get; set; }

        [Display(Name = "Notify Supervisor")]
        public bool NotifySupervisor { get; set; }

        [Display(Name = "Notify Research Coordinator")]
        public bool NotifyResearchCoordinator { get; set; }

        [Display(Name = "Notify Committee Members")]
        public bool NotifyCommitteeMembers { get; set; }

        [Display(Name = "Notify Head of Department")]
        public bool NotifyHeadOfDepartment { get; set; }

        [Display(Name = "Notify Public Visitor")]
        public bool NotifyPublicVisitor { get; set; }
    }
}
