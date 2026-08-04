using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

/// <summary>
/// System-wide settings. Administrators only, and grouped rather than key-by-key: the API validates
/// each group as a whole, so a combination that is individually plausible but jointly impossible,
/// such as more approvals than committee members, is rejected before it is stored.
/// </summary>
public class SettingsApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    // ---------- Committees ----------

    public Task<ApiResult<CommitteeSettingsDto>> GetCommitteesAsync(CancellationToken ct = default) =>
        GetAsync<CommitteeSettingsDto>("api/settings/committees", ct);

    public Task<ApiResult<CommitteeSettingsDto>> UpdateCommitteesAsync(
        UpdateCommitteeSettingsRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<CommitteeSettingsDto>("api/settings/committees", request, ct);

    // ---------- Passwords ----------

    public Task<ApiResult<PasswordSettingsDto>> GetPasswordsAsync(CancellationToken ct = default) =>
        GetAsync<PasswordSettingsDto>("api/settings/passwords", ct);

    public Task<ApiResult<PasswordSettingsDto>> UpdatePasswordsAsync(
        UpdatePasswordSettingsRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<PasswordSettingsDto>("api/settings/passwords", request, ct);

    // ---------- Notifications ----------

    public Task<ApiResult<NotificationSettingsDto>> GetNotificationsAsync(CancellationToken ct = default) =>
        GetAsync<NotificationSettingsDto>("api/settings/notifications", ct);

    public Task<ApiResult<NotificationSettingsDto>> UpdateNotificationsAsync(
        UpdateNotificationSettingsRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<NotificationSettingsDto>("api/settings/notifications", request, ct);

    // ---------- Ethics documents ----------

    public Task<ApiResult<IReadOnlyList<EthicsDocumentRequirementDto>>> GetEthicsDocumentsAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<EthicsDocumentRequirementDto>>("api/settings/ethics-documents", ct);

    public Task<ApiResult<EthicsDocumentRequirementDto>> CreateEthicsDocumentAsync(
        SaveEthicsDocumentRequirementRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<EthicsDocumentRequirementDto>("api/settings/ethics-documents", request, ct);

    public Task<ApiResult<EthicsDocumentRequirementDto>> UpdateEthicsDocumentAsync(
        Guid id, SaveEthicsDocumentRequirementRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<EthicsDocumentRequirementDto>($"api/settings/ethics-documents/{id}", request, ct);

    /// <summary>
    /// Retires a document or brings it back. There is no delete: one that has been asked of
    /// anyone is referenced by what they uploaded.
    /// </summary>
    public Task<ApiResult<EthicsDocumentRequirementDto>> SetEthicsDocumentActiveAsync(
        Guid id, bool isActive, CancellationToken ct = default) =>
        PutJsonAsync<EthicsDocumentRequirementDto>(
            $"api/settings/ethics-documents/{id}/active?isActive={(isActive ? "true" : "false")}", null, ct);

    // ---------- Access ----------

    public Task<ApiResult<AccessSettingsDto>> GetAccessAsync(CancellationToken ct = default) =>
        GetAsync<AccessSettingsDto>("api/settings/access", ct);

    public Task<ApiResult<AccessSettingsDto>> UpdateAccessAsync(
        UpdateAccessSettingsRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<AccessSettingsDto>("api/settings/access", request, ct);

    // ---------- Uploads ----------

    public Task<ApiResult<UploadSettingsDto>> GetUploadsAsync(CancellationToken ct = default) =>
        GetAsync<UploadSettingsDto>("api/settings/uploads", ct);

    public Task<ApiResult<UploadSettingsDto>> UpdateUploadsAsync(
        UpdateUploadSettingsRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<UploadSettingsDto>("api/settings/uploads", request, ct);

    // ---------- Where uploaded files are kept ----------

    public Task<ApiResult<StorageSettingsDto>> GetStorageAsync(CancellationToken ct = default) =>
        GetAsync<StorageSettingsDto>("api/settings/storage", ct);

    public Task<ApiResult<StorageSettingsDto>> UpdateStorageAsync(
        UpdateStorageSettingsRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<StorageSettingsDto>("api/settings/storage", request, ct);

    /// <summary>Tries a destination and reports back. A failure is an answer, not an error.</summary>
    public Task<ApiResult<StorageCheckResultDto>> CheckStorageAsync(
        string? provider = null, CancellationToken ct = default) =>
        PostJsonAsync<StorageCheckResultDto>(
            $"api/settings/storage/check{(string.IsNullOrWhiteSpace(provider) ? "" : $"?provider={Uri.EscapeDataString(provider)}")}",
            null, ct);

    /// <summary>
    /// Copies files stored elsewhere to the destination in force. Bounded per run, so a result
    /// with anything remaining means calling it again.
    /// </summary>
    public Task<ApiResult<StorageMigrationResultDto>> MigrateStorageAsync(CancellationToken ct = default) =>
        PostJsonAsync<StorageMigrationResultDto>("api/settings/storage/migrate", null, ct);

    // ---------- The institution ----------

    /// <summary>Anonymous on the API, so the footer and sign-in page can use it signed out.</summary>
    public Task<ApiResult<InstitutionSettingsDto>> GetInstitutionAsync(CancellationToken ct = default) =>
        GetAsync<InstitutionSettingsDto>("api/settings/institution", ct);

    public Task<ApiResult<InstitutionSettingsDto>> UpdateInstitutionAsync(
        UpdateInstitutionSettingsRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<InstitutionSettingsDto>("api/settings/institution", request, ct);

    // ---------- Deadlines ----------

    public Task<ApiResult<DeadlineSettingsDto>> GetDeadlinesAsync(CancellationToken ct = default) =>
        GetAsync<DeadlineSettingsDto>("api/settings/deadlines", ct);

    public Task<ApiResult<DeadlineSettingsDto>> UpdateDeadlinesAsync(
        UpdateDeadlineSettingsRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<DeadlineSettingsDto>("api/settings/deadlines", request, ct);

    // ---------- Research proposals ----------

    /// <summary>Anonymous on the API: the student's own screen has to say what it asks for.</summary>
    public Task<ApiResult<ProposalSettingsDto>> GetProposalsAsync(CancellationToken ct = default) =>
        GetAsync<ProposalSettingsDto>("api/settings/proposals", ct);

    public Task<ApiResult<ProposalSettingsDto>> UpdateProposalsAsync(
        UpdateProposalSettingsRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<ProposalSettingsDto>("api/settings/proposals", request, ct);

    // ---------- Comments on decisions ----------

    /// <summary>Readable by anyone signed in: every decision screen has to say what it requires.</summary>
    public Task<ApiResult<DecisionCommentSettingsDto>> GetDecisionCommentsAsync(CancellationToken ct = default) =>
        GetAsync<DecisionCommentSettingsDto>("api/settings/decision-comments", ct);

    public Task<ApiResult<DecisionCommentSettingsDto>> UpdateDecisionCommentsAsync(
        UpdateDecisionCommentSettingsRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<DecisionCommentSettingsDto>("api/settings/decision-comments", request, ct);
}
