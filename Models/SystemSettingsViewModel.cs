using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// Everything the System settings screen shows, in the four groups the API validates as
    /// units. Loaded together because they are one screen; saved separately, so a mistake in the
    /// mail server does not discard an edit to the password rules.
    /// </summary>
    public class SystemSettingsViewModel
    {
        public CommitteeSettingsDto Committees { get; set; } = new(0, 0, 0);

        public PasswordSettingsDto Passwords { get; set; } = new(10, true, true, true, true, 0, 5, 15);

        public NotificationSettingsDto Notifications { get; set; } =
            new(false, null, 587, null, false, true, null, null);

        /// <summary>
        /// Every ethics document, retired ones included, so an administrator can see what was
        /// once asked for and bring it back rather than recreating it under a name already taken.
        /// </summary>
        public IReadOnlyList<EthicsDocumentRequirementDto> EthicsDocuments { get; set; } = [];

        public AccessSettingsDto Access { get; set; } = new("InviteOnly", true, false, false, 14, 30, 14);

        public UploadSettingsDto Uploads { get; set; } = new(50, ".pdf,.doc,.docx,.zip");

        public StorageSettingsDto Storage { get; set; } =
            new("local", "App_Data/uploads", null, null, null, null, false, false, "uploads", false, 0);

        /// <summary>What testing the destination said, when the administrator has just asked.</summary>
        public StorageCheckResultDto? StorageCheck { get; set; }

        public InstitutionSettingsDto Institution { get; set; } =
            new("Auckland Institute of Studies", "@aisstudent.ac.nz", "@ais.ac.nz", null, null, null, null);

        public DeadlineSettingsDto Deadlines { get; set; } = new(14, 21, 30);

        public bool LoadFailed { get; set; }

        /// <summary>
        /// Which tab to open on. Carried through the redirect after a save so the administrator
        /// lands back where they were working rather than on the first tab.
        /// </summary>
        public string ActiveTab { get; set; } = "committees";

        /// <summary>
        /// Whether open self-registration can be chosen at all. The API refuses it outside a
        /// development environment, so offering the choice would only produce a rejection.
        /// </summary>
        public bool CanOpenRegistration { get; set; }

        public IEnumerable<EthicsDocumentRequirementDto> ActiveEthicsDocuments =>
            EthicsDocuments.Where(d => d.IsActive).OrderBy(d => d.SortOrder).ThenBy(d => d.Name);

        public IEnumerable<EthicsDocumentRequirementDto> RetiredEthicsDocuments =>
            EthicsDocuments.Where(d => !d.IsActive).OrderBy(d => d.Name);
    }
}
