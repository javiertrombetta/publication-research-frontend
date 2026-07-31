using System.ComponentModel.DataAnnotations;

namespace ResearchPublicationManagementSystem.Models
{
    public class CreatePublicationViewModel
    {
        public Guid ContainerId { get; set; }

        public Guid PublicationId { get; set; }

        public string? Status { get; set; }

        [Required(ErrorMessage = "Please enter a title.")]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter an abstract.")]
        public string Abstract { get; set; } = string.Empty;

        public string? PublicationType { get; set; }

        public int? PublicationYear { get; set; }

        public string? KeywordsCsv { get; set; }

        public string? ReviewerNotes { get; set; }

        public IFormFile? ResearchFile { get; set; }

        public bool HasUploadedVersion { get; set; }

        public int LatestVersionNumber { get; set; }
    }
}
