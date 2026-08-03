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

        /// <summary>
        /// What was answered to each of the screening questions, in the order they are asked, with
        /// null where one was left blank. Posted with the declaration and kept with it: the people
        /// who rule on that declaration were being shown the one-word answer and none of the
        /// working behind it.
        /// </summary>
        public string?[] Screening { get; set; } = new string?[Common.EthicsScreening.Questions.Length];
    }
}
