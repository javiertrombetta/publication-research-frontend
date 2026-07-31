namespace ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

public record EthicsDeclarationRequestDto(string Response);

public record EthicsDeclarationDto(Guid Id, Guid PublicationContainerId, string StudentResponse, DateTime DecidedAt);

public record EthicsApprovalDto(
    Guid Id,
    Guid PublicationContainerId,
    string Status,
    string? ReferenceNumber,
    DateTime? ApprovalDate,
    DateTime? ExpiryDate,
    bool? IsRequiredPerSupervisor,
    string? SupervisorDecisionComments,
    bool? IsRequiredPerCoordinator,
    string? CoordinatorDecisionComments,
    string? HeadOfDepartmentComments,
    DateTime? HeadOfDepartmentReviewedAt,
    DateTime? FinalDecisionAt);

public record EthicsDocumentDto(
    Guid Id,
    string DocumentType,
    string FileName,
    int Version,
    string Status,
    DateTime UploadedAt,
    string? ReviewComments);

public record EthicsGuidanceDto(string Title, string Content);

/// <summary>Matches the backend's EthicsDocumentType enum (Enums/EthicsEnums.cs) — string values sent as DocumentType.</summary>
public static class EthicsDocumentType
{
    public const string ApprovalCertificate = "ApprovalCertificate";
    public const string ApplicationForm = "ApplicationForm";
    public const string ParticipantConsentForm = "ParticipantConsentForm";
}

/// <summary>Matches the backend's EthicsStatus enum values (as returned in EthicsApprovalDto.Status).</summary>
public static class EthicsStatus
{
    /// <summary>Declaration made, nobody has ruled on it yet — the state an approval starts in.</summary>
    public const string PendingSupervisorDecision = "PendingSupervisorDecision";
    public const string NotRequired = "NotRequired";
    public const string PendingUpload = "PendingUpload";
    public const string PendingVerification = "PendingVerification";
    public const string Verified = "Verified";
}
