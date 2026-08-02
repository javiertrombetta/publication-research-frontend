using System.ComponentModel.DataAnnotations;

namespace ResearchPublicationManagementSystem.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }

        /// <summary>
        /// Whether this deployment can sign people in with their institutional Microsoft account.
        /// Set by the controller from the deployment's own configuration, not posted: it decides
        /// whether to offer a button, and a button that leads nowhere is worse than no button.
        /// </summary>
        public bool MicrosoftSignInAvailable { get; set; }
    }
}
