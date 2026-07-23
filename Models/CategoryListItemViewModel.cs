namespace ResearchPublicationManagementSystem.Models
{
    public class CategoryListItemViewModel
    {
        public int Id { get; set; }

        public string CategoryName { get; set; } = "";

        public string Description { get; set; } = "";

        public int PublicationCount { get; set; }

        public bool IsActive { get; set; }
    }
}