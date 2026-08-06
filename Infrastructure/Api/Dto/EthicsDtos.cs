namespace ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

public record EthicsDeclarationRequestDto(
    string Response,
    IReadOnlyList<EthicsScreeningAnswerDto>? Screening = null);

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
    DateTime? StudentDeclaredAt = null,
    /// <summary>
    /// The twenty screening questions the student worked through on the way to that answer, and
    /// what they said to each. Null for declarations made before these were kept.
    /// </summary>
    IReadOnlyList<EthicsScreeningAnswerDto>? StudentScreening = null);

/// <summary>One screening question as it was put to the student, and their answer.</summary>
public record EthicsScreeningAnswerDto(int Number, string Question, string Answer);

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

/// <param name="DocumentIds">Which of the documents are being asked for again. Empty, or left out, means all of them. Ignored when approving.</param>
public record CoordinatorDocumentReviewRequestDto(bool Approve, string Comments, IReadOnlyList<Guid>? DocumentIds = null);

/// <param name="DocumentIds">Which documents are being asked for again. Empty means all of them, and it is ignored when approving.</param>
public record CoordinatorFinalDecisionRequestDto(
    bool Approve, string Comments, IReadOnlyList<Guid>? DocumentIds = null);

/// <summary>
/// The supervisor's ruling on whether the research needs ethics approval documentation at all.
/// This is the first decision made on a declaration.
/// </summary>
public record SupervisorRequirementDecisionRequestDto(bool IsRequired, string Comments);

/// <summary>Accepting the student's ethics documents, or sending them back for revision.</summary>
public record DocumentReviewDecisionRequestDto(
    bool Accept,
    string Comments,
    /// <summary>
    /// Which of the uploaded documents are being asked for again. Empty means all of them, which
    /// is what a reviewer who has not singled any out is saying. Ignored when accepting.
    /// </summary>
    IReadOnlyList<Guid>? DocumentIds = null);

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

/// <summary>
/// One publication's whole ethics picture, as the queues ask for a page of them at once. See the
/// API's own remarks: asking per row made a screen's cost follow the size of the department.
/// </summary>
public record ContainerEthicsDto(
    Guid PublicationContainerId,
    EthicsApprovalDto Approval,
    IReadOnlyList<EthicsDocumentDto> Documents);

