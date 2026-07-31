namespace ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

/// <summary>Mirrors the backend's {success,data,message,errors} envelope (Common/ApiResponse.cs).</summary>
public class ApiResponseEnvelope<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public IReadOnlyList<string>? Errors { get; set; }
}

public record UserSummaryDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string Status,
    IReadOnlyList<string> Roles,
    bool HasProfilePhoto);

public record CommentsRequestDto(string Comments);
