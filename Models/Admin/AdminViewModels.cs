using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>Institution-wide figures, plus what needs an administrator's hand right now.</summary>
    public class AdminDashboardViewModel
    {
        public DashboardSummaryDto? Summary { get; set; }

        /// <summary>Papers under review with no evaluation committee. Nothing moves until one exists.</summary>
        public int PapersAwaitingCommittee { get; set; }

        public bool LoadFailed { get; set; }
    }

    /// <summary>Papers under review that still need an evaluation committee.</summary>
    public class AssignCommitteeViewModel
    {
        public List<AwaitingCommitteeItem> Items { get; set; } = [];

        /// <summary>Everyone who can sit on a committee, internal and external.</summary>
        /// <summary>
        /// Who may be appointed, as the API works it out. Not assembled from the directory here:
        /// the rule has several parts and an administrator chooses some of them.
        /// </summary>
        public IReadOnlyList<CommitteeCandidateDto> Members { get; set; } = [];

        /// <summary>
        /// The composition currently configured. Used only for publications that predate the
        /// figures being recorded per publication. Everything else states its own requirement.
        /// </summary>
        public CommitteeSettingsDto? CurrentRules { get; set; }

        public bool LoadFailed { get; set; }
    }

    public class AwaitingCommitteeItem
    {
        public AwaitingCommitteeDto Paper { get; set; } = null!;

        /// <summary>
        /// What this publication's committee must look like. Taken from the publication itself
        /// rather than from today's settings: it recorded the rules in force when it was opened,
        /// and the API will reject a committee that does not match them.
        /// </summary>
        public int RequiredInternal { get; set; }
        public int RequiredExternal { get; set; }

        public int RequiredTotal => RequiredInternal + RequiredExternal;
    }

    /// <summary>The user directory.</summary>
    public class UserDirectoryViewModel
    {
        public IReadOnlyList<UserListItemDto> Users { get; set; } = [];

        public string? Role { get; set; }
        public string? Status { get; set; }
        public string? Search { get; set; }

        public bool LoadFailed { get; set; }

        public bool HasFilters =>
            !string.IsNullOrWhiteSpace(Role) || !string.IsNullOrWhiteSpace(Status) || !string.IsNullOrWhiteSpace(Search);
    }

    /// <summary>One account, with the actions an administrator can take on it.</summary>
    public class UserDetailViewModel
    {
        public UserDetailDto User { get; set; } = null!;

        /// <summary>
        /// Needed when granting a role that belongs to a department. Without one the API refuses
        /// the change rather than leaving the account holding a role it cannot use.
        /// </summary>
        public IReadOnlyList<DepartmentDto> Departments { get; set; } = [];

        /// <summary>Roles that cannot be granted without saying which department.</summary>
        public static IReadOnlyList<string> DepartmentRoles =>
        [
            Common.RoleNames.Student,
            Common.RoleNames.Supervisor,
            Common.RoleNames.Coordinator,
            Common.RoleNames.HeadOfDepartment
        ];

        /// <summary>The operational roles that can be granted.</summary>
        public static IReadOnlyList<string> AssignableRoles =>
        [
            Common.RoleNames.Student,
            Common.RoleNames.Supervisor,
            Common.RoleNames.Coordinator,
            Common.RoleNames.HeadOfDepartment,
            Common.RoleNames.InternalCommitteeMember,
            Common.RoleNames.ExternalCommitteeMember,
            Common.RoleNames.Admin
        ];

        public bool IsEnabled => User.Status == "Enabled";
    }

    /// <summary>The institution-wide audit trail.</summary>
    public class AuditLogViewModel
    {
        public AuditLogQuery Query { get; set; } = new();

        public PagedResultDto<AuditLogEntryDto> Results { get; set; } =
            new([], 1, AuditLogQuery.DefaultPageSize, 0);

        public bool LoadFailed { get; set; }

        public bool HasFiltersApplied => Query.HasFilters;

        /// <summary>The filters as route values, so paging keeps the current view.</summary>
        public Dictionary<string, string?> RouteValues(int? page = null)
        {
            var values = new Dictionary<string, string?>();

            if (!string.IsNullOrWhiteSpace(Query.EntityType)) values["entityType"] = Query.EntityType;
            if (Query.UserId is not null) values["userId"] = Query.UserId.ToString();
            if (Query.From is not null) values["from"] = Query.From.Value.ToString("yyyy-MM-dd");
            if (Query.To is not null) values["to"] = Query.To.Value.ToString("yyyy-MM-dd");
            if (page is not null && page > 1) values["page"] = page.Value.ToString();

            return values;
        }
    }

    /// <summary>How many committee members of each type a publication needs by default.</summary>
    public class CommitteeSettingsViewModel
    {
        public IReadOnlyList<CommitteeRoleConfigDto> Config { get; set; } = [];

        public bool LoadFailed { get; set; }
    }

    /// <summary>The new-account form, with the departments a student or coordinator can belong to.</summary>
    public class CreateUserViewModel
    {
        public CreateUserRequestDto Request { get; set; } = new();

        public IReadOnlyList<DepartmentDto> Departments { get; set; } = [];

        /// <summary>Every role an administrator can create an account as.</summary>
        public static IReadOnlyList<string> Roles => UserDetailViewModel.AssignableRoles;
    }
}
