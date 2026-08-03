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
    DateTime? FinalDecisionAt,
    /// <summary>
    /// What the student answered when asked whether their research involves people: Yes, No or
    /// Unsure, or null before they have said. It is what a supervisor is ruling on.
    /// </summary>
    string? StudentDeclaration = null,
    DateTime? StudentDeclaredAt = null);

public record EthicsDocumentDto(
    Guid Id,
    string DocumentType,
    string FileName,
    int Version,
    string Status,
    DateTime UploadedAt,
    string? ReviewComments);

public record EthicsGuidanceDto(string Title, string Content);

/// <summary>Matches the backend's EthicsStatus enum values (as returned in EthicsApprovalDto.Status).</summary>
public static class EthicsStatus
{
    /// <summary>Declaration made, nobody has ruled on it yet, the state an approval starts in.</summary>
    public const string PendingSupervisorDecision = "PendingSupervisorDecision";
    public const string NotRequired = "NotRequired";
    public const string PendingUpload = "PendingUpload";
    public const string PendingVerification = "PendingVerification";
    public const string Verified = "Verified";
}

/// <summary>
/// The Coordinator's answer to a Supervisor saying no ethics documentation is needed:
/// RequireDocumentation overrides them and asks the student to upload it after all.
/// </summary>
public record CoordinatorNotRequiredReviewRequestDto(bool RequireDocumentation, string Comments);

public record CoordinatorDocumentReviewRequestDto(bool Approve, string Comments);

public record CoordinatorFinalDecisionRequestDto(bool Approve, string Comments);

/// <summary>
/// The supervisor's ruling on whether the research needs ethics approval documentation at all.
/// This is the first decision made on a declaration.
/// </summary>
public record SupervisorRequirementDecisionRequestDto(bool IsRequired, string Comments);

/// <summary>Accepting the student's ethics documents, or sending them back for revision.</summary>
public record DocumentReviewDecisionRequestDto(bool Accept, string Comments);

/// <summary>Matches the backend's EthicsDocumentStatus enum values.</summary>
public static class EthicsDocumentStatus
{
    public const string PendingReview = "PendingReview";
    public const string Accepted = "Accepted";
    public const string RevisionRequested = "RevisionRequested";
}

/// <summary>
/// The Head of Department's comments on ethics documentation. Comments only: they do not accept
/// or reject, they record an opinion for the coordinator's final decision.
/// </summary>
public record HeadOfDepartmentReviewRequestDto(string Comments);

/// <summary>
/// One document this publication has been asked for, and whether it has arrived. Carries the
/// requirement's id because that is what an upload is addressed to, and names can be edited.
/// </summary>
public record RequiredEthicsDocumentDto(
    Guid RequirementId,
    string Name,
    string? Description,
    int SortOrder,
    bool IsSatisfied);
