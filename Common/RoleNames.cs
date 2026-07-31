namespace ResearchPublicationManagementSystem.Common;

/// <summary>Mirrors the backend's Common/RoleNames.cs — the canonical role name strings issued as JWT/cookie role claims.</summary>
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string HeadOfDepartment = "HeadOfDepartment";
    public const string Coordinator = "Coordinator";
    public const string Supervisor = "Supervisor";
    public const string InternalCommitteeMember = "InternalCommitteeMember";
    public const string ExternalCommitteeMember = "ExternalCommitteeMember";
    public const string Student = "Student";
    public const string Staff = "Staff";

    /// <summary>Every role, in the order they appear in the workflow. Mirrors the backend's list.</summary>
    public static readonly string[] All =
    [
        Student, Supervisor, Coordinator, HeadOfDepartment,
        InternalCommitteeMember, ExternalCommitteeMember, Admin, Staff
    ];
}
