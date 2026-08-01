using System.ComponentModel.DataAnnotations;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    public class SignupViewModel
    {
        [Required(ErrorMessage = "First name is required.")]
        [MaxLength(150)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [MaxLength(150)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [MinLength(10, ErrorMessage = "Password must be at least 10 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? InstitutionalId { get; set; }

        // Only required for @aisstudent.ac.nz addresses, enforced server-side by the backend,
        // shown/hidden client-side based on the typed email domain (UX only).
        public string? StudentIdNumber { get; set; }
        public string? Programme { get; set; }
        public string? Cohort { get; set; }
        public Guid? DepartmentId { get; set; }

        public IReadOnlyList<DepartmentDto> DepartmentOptions { get; set; } = [];
    }
}
