namespace ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

public record ProposalDto(
    Guid Id,
    Guid PublicationContainerId,
    string Title,
    string Abstract,
    string Status,
    DateTime? SubmittedAt);

public record SaveProposalRequestDto(string Title, string Abstract);
