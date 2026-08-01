using System.Text.Json;

namespace ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

public record UserDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? InstitutionalId,
    string Status,
    string AuthProvider,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt,
    JsonElement? Profile,
    bool HasProfilePhoto,
    bool IsAvailable = true);

/// <summary>Whether this person is taking work on. Theirs to set, unlike Status.</summary>
public record SetAvailabilityRequestDto(bool IsAvailable);

public record StudentProfileSummaryDto(
    Guid Id,
    string StudentIdNumber,
    string Programme,
    string Cohort,
    Guid DepartmentId,
    string DepartmentName,
    Guid? PreferredSupervisorId,
    string? Orcid,
    IReadOnlyList<string> ResearchAreas);

public record UpdateMyProfileRequestDto(
    string FirstName,
    string LastName,
    string? Programme,
    string? Cohort,
    Guid? PreferredSupervisorId,
    string? Orcid,
    IReadOnlyList<Guid>? ResearchAreaIds,
    string? AreasOfExpertise,
    string? ResearchInterests);

/// <summary>A user as they appear in a listing.</summary>
public record UserListItemDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Status,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}
