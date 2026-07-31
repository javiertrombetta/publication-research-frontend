using System.ComponentModel.DataAnnotations;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>The invitations an administrator has sent, and the form for sending another.</summary>
    public class InvitationsViewModel
    {
        public IReadOnlyList<UserInvitationDto> Invitations { get; set; } = [];

        /// <summary>
        /// Needed for the roles that belong to one. External committee members are the exception,
        /// which is the whole reason they cannot register themselves.
        /// </summary>
        public IReadOnlyList<DepartmentDto> Departments { get; set; } = [];

        public bool LoadFailed { get; set; }

        public IEnumerable<UserInvitationDto> Pending =>
            Invitations.Where(i => i.Status == "Pending");

        public IEnumerable<UserInvitationDto> Settled =>
            Invitations.Where(i => i.Status != "Pending");
    }

    /// <summary>
    /// Accepting an invitation. The role is not here on purpose — it comes from the invitation,
    /// so accepting one can never be a way to award yourself a role nobody offered.
    /// </summary>
    public class AcceptInvitationViewModel
    {
        public string Token { get; set; } = string.Empty;

        /// <summary>Null when the token is bad, expired or already used; the view says why.</summary>
        public InvitationPreviewDto? Invitation { get; set; }

        public string? Problem { get; set; }

        [Required(ErrorMessage = "Choose a password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type your password again.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
