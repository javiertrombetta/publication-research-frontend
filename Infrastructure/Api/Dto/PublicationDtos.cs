namespace ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

public record PublicationDto(
    Guid Id,
    Guid PublicationContainerId,
    string Title,
    string Abstract,
    string? PublicationType,
    int? PublicationYear,
    string Status,
    bool IsPublished,
    DateTime? PublishedAt,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<string> ResearchAreas,
    /// <summary>
    /// Whose paper it is. Null wherever the reader is the author or already knows. Filled in on
    /// the queues that ask somebody else to judge a paper, which let them search and order by the
    /// student and so have to be able to name one.
    /// </summary>
    string? StudentName = null);

public record UpdatePublicationMetadataRequestDto(
    string Title,
    string Abstract,
    string? PublicationType,
    int? PublicationYear,
    IReadOnlyList<string>? Keywords,
    IReadOnlyList<Guid>? ResearchAreaIds);

public record PublicationVersionDto(
    Guid Id,
    int VersionNumber,
    string FileName,
    string? SupplementaryFilesPath,
    string? ReviewerNotes,
    string UploadedByName,
    DateTime UploadedAt);

public record PublishDecisionRequestDto(bool Publish, string? Comments);

/// <summary>Matches the backend's PublicationStatus enum values (as returned in PublicationDto.Status).</summary>
public static class PublicationStatus
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string EthicsVerification = "EthicsVerification";
    public const string UnderReview = "UnderReview";
    public const string RevisionsRequested = "RevisionsRequested";
    public const string Resubmitted = "Resubmitted";
    public const string Accepted = "Accepted";
    public const string Published = "Published";
}

public record PaperReviewDecisionRequestDto(bool Accept, string Comments);

/// <summary>One recorded review of a paper version.</summary>
public record ReviewDto(
    Guid Id,
    string ReviewerName,
    string ReviewerType,
    string Decision,
    string Comments,
    DateTime ReviewedAt);

/// <summary>
/// A research paper waiting for an evaluation committee: approved by its supervisor, with none
/// appointed. Carries the composition the publication was opened under, which is what the API
/// will judge the administrator's selection against.
/// </summary>
public record AwaitingCommitteeDto(
    Guid Id,
    Guid PublicationContainerId,
    string Title,
    string Abstract,
    string StudentName,
    int? RequiredReviewerMembers,
    int? RequiredExternalCommitteeMembers);
