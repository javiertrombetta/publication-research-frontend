using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    /// <summary>
    /// The ethics documents this publication has been asked for.
    ///
    /// The list is not fixed. It used to be three named fields, but an administrator now decides
    /// what the ethics stage asks for, and each publication keeps the list it was given, so a
    /// student halfway through is never asked for a document that did not exist when they started.
    /// The form is therefore built from <see cref="Required"/> rather than from properties known at
    /// compile time.
    /// </summary>
    public class UploadEthicsDocumentsViewModel
    {
        public Guid ContainerId { get; set; }

        /// <summary>What this publication owes, and what has already been accepted.</summary>
        public IReadOnlyList<RequiredEthicsDocumentDto> Required { get; set; } = [];

        /// <summary>
        /// The files posted back, keyed by the requirement each one answers. A dictionary because
        /// the field names are only known at run time; the model binder fills it from inputs
        /// named <c>Files[requirement-id]</c>.
        /// </summary>
        public Dictionary<Guid, IFormFile?> Files { get; set; } = [];

        public IReadOnlyList<EthicsDocumentDto> ExistingDocuments { get; set; } = [];

        public bool IsComplete => Required.Count > 0 && Required.All(r => r.IsSatisfied);
    }
}
