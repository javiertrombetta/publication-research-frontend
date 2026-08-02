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
    bool IsAvailable = true,
    /// <summary>Light or dark, as they last chose it. Null until they have.</summary>
    string? ThemePreference = null);

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
    DateTime CreatedAt,
    /// <summary>
    /// Whether this person is taking work on. Separate from Status: a supervisor away for a month
    /// still has an account, and disabling it instead would lock them out of their own work.
    /// Meaningless on an account with no operational role, since nothing ever chooses those.
    /// </summary>
    bool IsAvailable = true)
{
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>
    /// Whether availability says anything about this account. Nothing in the system picks a
    /// student or a placeholder Staff account for work, so the flag they carry has never been
    /// asked and reading it as "not taking work on" would invent a refusal nobody made.
    /// </summary>
    public bool ChoosesWork => Roles.Any(Common.RoleNames.Operational.Contains);
}
