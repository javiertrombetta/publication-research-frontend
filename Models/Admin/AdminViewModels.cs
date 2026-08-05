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

        /// <summary>Everyone who can sit on a committee, reviewers and external members.</summary>
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

        /// <summary>
        /// Committees already sitting. The screen used to list only papers with none, so an
        /// appointed committee left every screen there was and a member who had to be replaced
        /// needed the database.
        /// </summary>
        public IReadOnlyList<CommitteeDto> InProgress { get; set; } = [];

        public int InProgressTotal { get; set; }

        public PagerViewModel? InProgressPager { get; set; }

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
        public int RequiredReviewers { get; set; }
        public int RequiredExternal { get; set; }

        public int RequiredTotal => RequiredReviewers + RequiredExternal;
    }

    /// <summary>The user directory.</summary>
    public class UserDirectoryViewModel
    {
        public IReadOnlyList<UserListItemDto> Users { get; set; } = [];

        public string? Role { get; set; }
        public string? Status { get; set; }
        public string? Search { get; set; }

        public bool LoadFailed { get; set; }

        /// <summary>
        /// How many accounts match, and which page of them this is. The directory used to be
        /// returned entire, which is fine for a demonstration dataset and not for an institution.
        /// </summary>
        public int TotalCount { get; set; }
        public PagerViewModel? Pager { get; set; }

        public string? Sort { get; set; }
        public bool Descending { get; set; }

        public bool HasFilters =>
            !string.IsNullOrWhiteSpace(Role) || !string.IsNullOrWhiteSpace(Status) || !string.IsNullOrWhiteSpace(Search);

        /// <summary>Everything the listing is filtered and ordered by, so paging keeps all of it.</summary>
        public Dictionary<string, string?> RouteValues()
        {
            var values = new Dictionary<string, string?>();
            if (!string.IsNullOrWhiteSpace(Role)) values["role"] = Role;
            if (!string.IsNullOrWhiteSpace(Status)) values["status"] = Status;
            if (!string.IsNullOrWhiteSpace(Search)) values["search"] = Search;
            if (!string.IsNullOrWhiteSpace(Sort)) values["sort"] = Sort;
            if (Descending) values["desc"] = "true";
            return values;
        }

        public SortableColumnViewModel Column(string column, string label, bool descendingFirst = false) => new()
        {
            Controller = "Users",
            Action = "Index",
            Column = column,
            Label = label,
            CurrentSort = Sort,
            CurrentDescending = Descending,
            DescendingFirst = descendingFirst,
            RouteValues = RouteValues()
                .Where(v => v.Key is not ("sort" or "desc"))
                .ToDictionary(v => v.Key, v => v.Value)
        };
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

        /// <summary>
        /// The departments this person is already in, so the picker opens on what is true rather
        /// than on nothing. Changing a supervisor's role without this would quietly offer to move
        /// them out of every department they are in.
        /// </summary>
        public IReadOnlyList<Guid> CurrentDepartmentIds { get; set; } = [];

        /// <summary>Roles that cannot be granted without saying which department.</summary>
        public static IReadOnlyList<string> DepartmentRoles =>
        [
            Common.RoleNames.Student,
            Common.RoleNames.Coordinator,
            Common.RoleNames.HeadOfDepartment
        ];

        /// <summary>
        /// The roles that belong to one department or several.
        ///
        /// Supervising and reviewing are not exclusive, so the form offers a list rather than a
        /// choice. An external committee member is in neither list: they come from another
        /// institution, so asking them for a department would be asking the wrong question.
        /// </summary>
        public static IReadOnlyList<string> MultiDepartmentRoles =>
        [
            Common.RoleNames.Supervisor,
            Common.RoleNames.Reviewer
        ];

        /// <summary>The operational roles that can be granted.</summary>
        public static IReadOnlyList<string> AssignableRoles =>
        [
            Common.RoleNames.Student,
            Common.RoleNames.Supervisor,
            Common.RoleNames.Coordinator,
            Common.RoleNames.HeadOfDepartment,
            Common.RoleNames.Reviewer,
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

    /// <summary>
    /// Publications still under way, and who is responsible for each.
    ///
    /// Every step waits on somebody named on the publication, so a person who leaves or falls ill
    /// stops it. This is where an administrator names somebody else.
    /// </summary>
    public class AssignmentsViewModel
    {
        public IReadOnlyList<PublicationContainerDto> Publications { get; set; } = [];

        public int TotalCount { get; set; }

        public PagerViewModel? Pager { get; set; }

        public string? Search { get; set; }

        /// <summary>
        /// Who may supervise. Not scoped by department: a supervisor is chosen for what they know
        /// about the subject, and the institution lets them hold posts in more than one department.
        /// </summary>
        public IReadOnlyList<UserListItemDto> Supervisors { get; set; } = [];

        /// <summary>
        /// Who may coordinate and who may take the ethics review, per department.
        ///
        /// Both posts are held in a department, and their authority over a publication comes from
        /// the student being in it, so the choice on each row is that student's department only.
        /// Fetched per department on the page rather than per row: two departments cover twenty
        /// publications.
        /// </summary>
        public Dictionary<Guid, DepartmentMembersDto> ByDepartment { get; set; } = [];

        public IReadOnlyList<DepartmentPersonDto> CoordinatorsFor(Guid? departmentId) =>
            departmentId is { } id && ByDepartment.TryGetValue(id, out var members)
                ? members.Coordinators
                : [];

        public IReadOnlyList<DepartmentPersonDto> HeadsFor(Guid? departmentId) =>
            departmentId is { } id && ByDepartment.TryGetValue(id, out var members)
                ? members.HeadsOfDepartment
                : [];

        public bool LoadFailed { get; set; }

        public Dictionary<string, string?> RouteValues() => new()
        {
            ["search"] = Search
        };
    }
}
