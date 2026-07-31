using System.ComponentModel.DataAnnotations;

namespace ResearchPublicationManagementSystem.Models
{
    public class StudentProfileViewModel
    {
        [Required]
        [MaxLength(150)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public bool HasProfilePhoto { get; set; }

        // Read-only in the UI — not part of UpdateMyProfileRequest, set by an Admin instead.
        public string? StudentIdNumber { get; set; }
        public string? DepartmentName { get; set; }

        public string? Programme { get; set; }
        public string? Cohort { get; set; }
        public string? Orcid { get; set; }

        public IReadOnlyList<string> ResearchAreas { get; set; } = [];
    }
}
