using System.ComponentModel.DataAnnotations;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Models
{
    public class EthicsDeclarationPageViewModel
    {
        public Guid ContainerId { get; set; }

        [Required(ErrorMessage = "Please select Yes, No, or Unsure.")]
        public string? Response { get; set; }

        public EthicsGuidanceDto? Guidance { get; set; }
    }
}
