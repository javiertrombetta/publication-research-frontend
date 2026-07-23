using System.ComponentModel.DataAnnotations;

namespace ResearchPublicationManagementSystem.Models
{
    public class EditUserViewModel
    {
        public string Id { get; set; } = "";

        [Required]
        public string FullName { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        public string Phone { get; set; } = "";

        public string Department { get; set; } = "";

        [Required]
        public string Role { get; set; } = "";

        public bool IsActive { get; set; } = true;
    }
}
