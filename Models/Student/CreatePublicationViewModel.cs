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

        public Guid? LatestVersionId { get; set; }

        /// <summary>
        /// Whether this paper is still the student's to change.
        ///
        /// A draft is, and so is one sent back for revisions, which is what being sent back means.
        /// Anything else is with somebody: a supervisor reading it, a committee evaluating it, a
        /// coordinator deciding on it, or an outcome already recorded. Editing then would mean
        /// people reviewing a paper that is no longer the one in front of them. The API refuses it
        /// either way; this is so the screen does not offer what will be refused.
        /// </summary>
        public bool IsEditable =>
            Status is null
            or Infrastructure.Api.Dto.PublicationStatus.Draft
            or Infrastructure.Api.Dto.PublicationStatus.RevisionsRequested;
    }
}
