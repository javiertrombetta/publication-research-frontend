namespace ResearchPublicationManagementSystem.Common;

/// <summary>Mirrors the backend's Common/RoleNames.cs: the canonical role name strings issued as JWT/cookie role claims.</summary>
public static class RoleNames
{
    public const string Admin = "Admin";
    public const string HeadOfDepartment = "HeadOfDepartment";
    public const string Coordinator = "Coordinator";
    public const string Supervisor = "Supervisor";
    public const string Reviewer = "Reviewer";
    public const string ExternalCommitteeMember = "ExternalCommitteeMember";
    public const string Student = "Student";
    public const string Staff = "Staff";

    /// <summary>
    /// Not an Identity role: the answer to "whose turn is it" when the turn belongs to the
    /// evaluation committee as a body rather than to any one person. Two roles sit on a committee,
    /// so neither of their names would be the truthful answer.
    /// </summary>
    public const string EvaluationCommittee = "EvaluationCommittee";



    /// <summary>
    /// Everyone who may sit on an evaluation committee, and so everyone the committee screens have
    /// to let in. Mirrors the API, which is what actually enforces it.
    ///
    /// Students are excluded because a committee judges their work, and Staff because it is the
    /// placeholder an institutional account holds until an administrator says what it is: not a
    /// job, so nobody to ask yet.
    /// </summary>
    /// <summary>
    /// The roles that mean a job here: everyone the system can choose for something. Mirrors the
    /// API, which is what enforces it.
    ///
    /// A student is the subject of the work rather than somebody it is handed to, and Staff is the
    /// placeholder an institutional address is given on the way in, before an administrator says
    /// what the person is. Neither is ever picked, so neither is asked whether they are available.
    /// </summary>
    public static readonly string[] Operational =
    [
        Admin, HeadOfDepartment, Coordinator, Supervisor,
        Reviewer, ExternalCommitteeMember
    ];

    public const string CommitteeEligibleRoles =
        $"{Admin},{HeadOfDepartment},{Coordinator},{Supervisor},{Reviewer},{ExternalCommitteeMember}";

    /// <summary>Every role, in the order they appear in the workflow. Mirrors the backend's list.</summary>
    public static readonly string[] All =
    [
        Student, Supervisor, Coordinator, HeadOfDepartment,
        Reviewer, ExternalCommitteeMember, Admin, Staff
    ];
}
