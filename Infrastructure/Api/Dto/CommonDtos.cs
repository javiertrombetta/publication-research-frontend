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
    bool HasProfilePhoto,
    /// <summary>
    /// How this person has arranged their sidebar, as routes separated by spaces, or null if they
    /// never have. Put into the session at sign-in so the menu can be drawn in their order on
    /// every page without asking for it each time.
    /// </summary>
    string? SidebarOrder = null);

public record CommentsRequestDto(string Comments);
