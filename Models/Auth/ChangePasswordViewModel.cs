using System.ComponentModel.DataAnnotations;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// Changing your own password. Deliberately carries no length or complexity rules: those are
    /// set by an administrator and can change, so the API is the only thing that knows them.
    /// Duplicating a minimum length here would go stale the first time it was raised, and would
    /// reject a password the server would have accepted.
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Enter your current password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a new password.")]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>
        /// Compared in the controller rather than with [Compare]: the mismatch message should
        /// read as a sentence about the two passwords, not about a field name.
        /// </summary>
        [Required(ErrorMessage = "Type your new password again.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
