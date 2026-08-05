namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// What a student sees on their own profile. Every field is read-only: this is the
    /// institution's record of them, and their work is filed and assessed against it. An
    /// administrator maintains it.
    ///
    /// The one thing a student changes is their photo, and that is not a field here. It goes
    /// through ProfileController, and only <see cref="HasProfilePhoto"/> says whether there is one
    /// to show. Nothing on this page is ever posted back, so there is nothing to validate.
    /// </summary>
    public class StudentProfileViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public bool HasProfilePhoto { get; set; }

        public string? StudentIdNumber { get; set; }
        public string? DepartmentName { get; set; }
        public string? Programme { get; set; }
        public string? Cohort { get; set; }

        /// <summary>
        /// The student's own registration with ORCID, where they have one. Not everybody has, so
        /// this is blank rather than absent, and the screen says so instead of hiding the row.
        /// </summary>
        public string? Orcid { get; set; }

        /// <summary>
        /// What this student says they work on. The API has returned these all along and no screen
        /// read them, so the tags a supervisor would actually match a proposal against were
        /// invisible to everybody including the student who chose them.
        /// </summary>
        public IReadOnlyList<string> ResearchAreas { get; set; } = [];
    }
}
