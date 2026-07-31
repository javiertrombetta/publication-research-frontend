using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    public class UploadEthicsDocumentsViewModel
    {
        public Guid ContainerId { get; set; }

        public IFormFile? ApplicationFormFile { get; set; }

        public IFormFile? ParticipantConsentFormFile { get; set; }

        public IFormFile? ApprovalCertificateFile { get; set; }

        public IReadOnlyList<EthicsDocumentDto> ExistingDocuments { get; set; } = [];
    }
}
