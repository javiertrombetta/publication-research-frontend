namespace ResearchPublicationManagementSystem.Infrastructure.Api.Dto
{
    /// <summary>
    /// Committee composition. These govern publications opened from the moment they change:
    /// each Publication Container keeps the figures that were in force when it was created, so
    /// research already under way is judged by the rules it started under.
    /// </summary>
    /// <summary>
    /// How a committee is composed, and who it may be composed of. SelectableRoles is not a
    /// setting: it is every role that could be chosen, sent by the API so the screen offers the
    /// real list rather than one written out again here.
    /// </summary>
    public record CommitteeSettingsDto(
        int InternalMembers,
        int ExternalMembers,
        int MinimumApprovals,
        IReadOnlyList<string> CandidateRoles,
        IReadOnlyList<Guid> ExcludedUserIds,
        IReadOnlyList<string> SelectableRoles);

    public record UpdateCommitteeSettingsRequestDto(
        int InternalMembers,
        int ExternalMembers,
        int MinimumApprovals,
        IReadOnlyList<string>? CandidateRoles = null,
        IReadOnlyList<Guid>? ExcludedUserIds = null);

    /// <summary>
    /// What counts as an acceptable password, how long one lasts, and when an account locks.
    /// <see cref="ExpiryDays"/> of zero means passwords never expire.
    /// </summary>
    public record PasswordSettingsDto(
        int MinimumLength,
        bool RequireDigit,
        bool RequireUppercase,
        bool RequireLowercase,
        bool RequireSymbol,
        int ExpiryDays,
        int LockoutAttempts,
        int LockoutMinutes);

    public record UpdatePasswordSettingsRequestDto(
        int MinimumLength,
        bool RequireDigit,
        bool RequireUppercase,
        bool RequireLowercase,
        bool RequireSymbol,
        int ExpiryDays,
        int LockoutAttempts,
        int LockoutMinutes);

    /// <summary>
    /// The mail server, and whether notifications are emailed at all. The stored SMTP password is
    /// never sent back. <see cref="HasPassword"/> only says whether one exists.
    /// </summary>
    public record NotificationSettingsDto(
        bool EmailEnabled,
        string? SmtpHost,
        int SmtpPort,
        string? SmtpUsername,
        bool HasPassword,
        bool UseSsl,
        string? FromAddress,
        string? FromName);

    /// <summary>
    /// A null <see cref="SmtpPassword"/> keeps whatever is stored, so changing the port does not
    /// require retyping a password nobody can read back. An empty string clears it.
    /// </summary>
    public record UpdateNotificationSettingsRequestDto(
        bool EmailEnabled,
        string? SmtpHost,
        int SmtpPort,
        string? SmtpUsername,
        string? SmtpPassword,
        bool UseSsl,
        string? FromAddress,
        string? FromName);

    /// <summary>
    /// One document the ethics stage asks students for. <see cref="IsInUse"/> means someone has
    /// already been asked for it, so it can be retired but never removed.
    /// </summary>
    public record EthicsDocumentRequirementDto(
        Guid Id,
        string Name,
        string? Description,
        int SortOrder,
        bool IsActive,
        bool IsInUse);

    public record SaveEthicsDocumentRequirementRequestDto(string Name, string? Description, int SortOrder);

    /// <summary>
    /// Who may get an account, and how long a session lasts.
    ///
    /// <see cref="IsEnvironmentDefault"/> means nobody has chosen and the mode is coming from the
    /// hosting environment, open in development and invite-only anywhere else. <see
    /// cref="AzureSsoConfigured"/> is a fact about the server, not a setting: it says whether a
    /// Microsoft Entra tenant exists to sign in against.
    /// </summary>
    public record AccessSettingsDto(
        string RegistrationMode,
        bool IsEnvironmentDefault,
        bool AzureSsoEnabled,
        bool AzureSsoConfigured,
        int InvitationValidDays,
        int AccessTokenMinutes,
        int RefreshTokenDays,
        bool PublicCatalogueEnabled = true,
        /// <summary>
        /// Whether this deployment will accept open registration. The API's answer, not this
        /// application's guess: the rule turns on the API's configuration, and the two are separate
        /// services once deployed.
        /// </summary>
        bool CanOpenRegistration = false);

    public record UpdateAccessSettingsRequestDto(
        string RegistrationMode,
        bool AzureSsoEnabled,
        int InvitationValidDays,
        int AccessTokenMinutes,
        int RefreshTokenDays,
        bool PublicCatalogueEnabled = true,
        /// <summary>
        /// Whether this deployment will accept open registration. The API's answer, not this
        /// application's guess: the rule turns on the API's configuration, and the two are separate
        /// services once deployed.
        /// </summary>
        bool CanOpenRegistration = false);

    public record UploadSettingsDto(int MaxMegabytes, string AllowedExtensions);

    /// <summary>
    /// Where uploaded files are kept. Changing it points new uploads somewhere else and nothing
    /// more: every stored file records the destination that wrote it, so what is already there
    /// keeps opening from where it is.
    /// </summary>
    public record StorageSettingsDto(
        string Provider,
        string LocalPath,
        string? S3Bucket,
        string? S3Region,
        string? S3ServiceUrl,
        string? S3AccessKeyId,
        bool S3SecretKeySet,
        bool S3ForcePathStyle,
        string AzureContainer,
        bool AzureConnectionStringSet,
        int FilesElsewhere = 0)
    {
        public bool IsLocal => Provider == "local";
        public bool IsDatabase => Provider == "database";
        public bool IsS3 => Provider == "s3";
        public bool IsAzure => Provider == "azure-blob";

        public string ProviderName => Provider switch
        {
            "database" => "The database",
            "s3" => "S3 or compatible object storage",
            "azure-blob" => "Azure Blob Storage",
            _ => "A directory on the server"
        };
    }

    /// <summary>The secrets are null to keep whatever is stored, which is what the form sends unless one is retyped.</summary>
    public record UpdateStorageSettingsRequestDto(
        string Provider,
        string? LocalPath,
        string? S3Bucket,
        string? S3Region,
        string? S3ServiceUrl,
        string? S3AccessKeyId,
        string? S3SecretKey,
        bool S3ForcePathStyle,
        string? AzureContainer,
        string? AzureConnectionString);

    public record StorageCheckResultDto(bool Reachable, string Message);

    /// <summary>What one run of the copy did. Remaining above zero means run it again.</summary>
    public record StorageMigrationResultDto(int Moved, int Remaining, IReadOnlyList<string> Problems);

    public record UpdateUploadSettingsRequestDto(int MaxMegabytes, string AllowedExtensions);

    /// <summary>
    /// The institution itself. Read anonymously, because the sign-in page, the footer and the
    /// public catalogue all need it before anyone has signed in.
    /// </summary>
    public record InstitutionSettingsDto(
        string Name,
        string StudentEmailDomain,
        string StaffEmailDomain,
        string? ItSupportEmail,
        string? ResearchEnquiriesEmail,
        string? PrivacyPolicyUrl,
        string? CurrentAcademicCycle,
        /// <summary>
        /// The institution's own website, where somebody who cannot be given an enquiries address
        /// can find how to get in touch.
        /// </summary>
        string? WebsiteUrl = null,
        /// <summary>
        /// Whether anyone may sign themselves up. Set under access settings and read-only here;
        /// it travels on this group because this is the one settings endpoint a signed-out
        /// visitor can call, and the sign-up page needs it.
        /// </summary>
        bool SelfRegistrationOpen = false,
    /// <summary>
    /// Whether the site shows a public catalogue at all. Rides on the anonymous response because
    /// it decides the landing page, which has to be settled before anyone has signed in.
    /// </summary>
    bool PublicCatalogueEnabled = true);

    public record UpdateInstitutionSettingsRequestDto(
        string Name,
        string StudentEmailDomain,
        string StaffEmailDomain,
        string? ItSupportEmail,
        string? ResearchEnquiriesEmail,
        string? PrivacyPolicyUrl,
        string? CurrentAcademicCycle,
        string? WebsiteUrl = null);

    /// <summary>How long each stage should take. Zero means nothing is ever reported late.</summary>
    public record DeadlineSettingsDto(int SupervisorResponseDays, int EthicsReviewDays, int CommitteeReviewDays);

    public record UpdateDeadlineSettingsRequestDto(int SupervisorResponseDays, int EthicsReviewDays, int CommitteeReviewDays);

    /// <summary>An invitation as an administrator sees it. The token is never returned.</summary>
    public record UserInvitationDto(
        Guid Id,
        string Email,
        string Role,
        string FirstName,
        string LastName,
        Guid? DepartmentId,
        string? DepartmentName,
        string InvitedByName,
        DateTime CreatedAt,
        DateTime ExpiresAt,
        DateTime? AcceptedAt,
        DateTime? RevokedAt,
        string Status);

    public record CreateInvitationRequestDto(
        string Email,
        string Role,
        string FirstName,
        string LastName,
        Guid? DepartmentId);

    /// <summary>What an invited person is shown before they accept.</summary>
    public record InvitationPreviewDto(
        string Email,
        string Role,
        string FirstName,
        string LastName,
        string InstitutionName,
        DateTime ExpiresAt);

    public record AcceptInvitationRequestDto(string Token, string Password);
}
