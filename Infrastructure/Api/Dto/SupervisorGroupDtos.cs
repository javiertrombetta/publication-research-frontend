namespace ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

/// <summary>
/// A coordinator's saved set of supervisors, used to fill in the Send proposals form in one go.
/// </summary>
/// <param name="AvailableCount">
/// How many of its members could actually be sent a proposal right now. Lower than MemberCount when
/// somebody has been disabled or has marked themselves as not taking work on.
/// </param>
public record SupervisorGroupDto(
    Guid Id,
    string Name,
    Guid OwnerId,
    string OwnerName,
    int MemberCount,
    int AvailableCount,
    IReadOnlyList<SupervisorGroupMemberDto> Members)
{
    /// <summary>
    /// The member ids as one attribute value, for the button that ticks them. Written here rather
    /// than in the view so the two screens that offer groups spell it the same way.
    /// </summary>
    public string MemberIdList => string.Join(",", Members.Select(m => m.SupervisorId));
}

public record SupervisorGroupMemberDto(Guid SupervisorId, string Name, bool IsAvailable);

public record SaveSupervisorGroupRequestDto(string Name, IReadOnlyList<Guid> SupervisorIds);

public record DeleteSupervisorGroupsRequestDto(IReadOnlyList<Guid> GroupIds, bool All = false);
