namespace ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

/// <param name="HeadOfDepartmentName">Whoever heads it, as one line. Several names where an institution has put more than one person at the top, and null where nobody is.</param>
public record DepartmentDto(Guid Id, string Name, string Code, string? HeadOfDepartmentName);

public record CreateDepartmentRequestDto(string Name, string Code);

public record UpdateDepartmentRequestDto(string Name, string Code);

/// <summary>Somebody in a department, as a screen listing them needs them.</summary>
public record DepartmentPersonDto(Guid UserId, string Name, string Email);

/// <summary>
/// Who is in a department, by the job they do in it.
///
/// Two of these are the department's own: its heads and its coordinators belong to it and nowhere
/// else. The other two are attachments, since a supervisor or a reviewer may be in several
/// departments at once, and are shown so an administrator sees the whole of a department at once.
/// </summary>
public record DepartmentMembersDto(
    Guid DepartmentId,
    string DepartmentName,
    IReadOnlyList<DepartmentPersonDto> HeadsOfDepartment,
    IReadOnlyList<DepartmentPersonDto> Coordinators,
    IReadOnlyList<DepartmentPersonDto> Supervisors,
    IReadOnlyList<DepartmentPersonDto> Reviewers);

/// <summary>
/// This department's heads and coordinators, as a whole list. Naming somebody moves them here from
/// wherever they were; leaving somebody out is refused rather than obeyed, because a head or a
/// coordinator with no department holds a job in nothing.
/// </summary>
public record SetDepartmentMembersRequestDto(
    IReadOnlyList<Guid> HeadOfDepartmentUserIds,
    IReadOnlyList<Guid> CoordinatorUserIds);
