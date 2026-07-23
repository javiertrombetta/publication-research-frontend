using System.ComponentModel.DataAnnotations;

namespace ResearchPublicationManagementSystem.Models
{
    public class CategoryViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = "";

        [Display(Name = "Description")]
        public string Description { get; set; } = "";

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
