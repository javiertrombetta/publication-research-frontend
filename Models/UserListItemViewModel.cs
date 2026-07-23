namespace ResearchPublicationManagementSystem.Models
{
    public class UserListItemViewModel
    {
        public string FullName { get; set; } = "";

        public string Email { get; set; } = "";

        public string Role { get; set; } = "";

        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
