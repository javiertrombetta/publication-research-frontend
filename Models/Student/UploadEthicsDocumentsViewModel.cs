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
        /// The name of the file input answering a given requirement. The field names are only known
        /// at run time, so they carry the requirement's id.
        ///
        /// There is deliberately no property here holding the posted files. A
        /// <c>Dictionary&lt;Guid, IFormFile&gt;</c> looks like the obvious way to receive them and
        /// silently never fills: the dictionary binder looks for form *values* under each key, and
        /// an uploaded file is not a value, it arrives in the request's file collection. The action
        /// reads them from there instead.
        /// </summary>
        public static string FieldNameFor(Guid requirementId) => $"Files[{requirementId}]";

        public IReadOnlyList<EthicsDocumentDto> ExistingDocuments { get; set; } = [];

        public bool IsComplete => Required.Count > 0 && Required.All(r => r.IsSatisfied);
    }
}
