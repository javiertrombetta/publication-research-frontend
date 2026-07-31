namespace ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

public record LoginRequestDto(string Email, string Password);

public record RegisterRequestDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? InstitutionalId,
    string? StudentIdNumber,
    string? Programme,
    string? Cohort,
    Guid? DepartmentId,
    IReadOnlyList<Guid>? ResearchAreaIds);

public record RefreshTokenRequestDto(string RefreshToken);

public record ForgotPasswordRequestDto(string Email);

public record ResetPasswordRequestDto(string Email, string Token, string NewPassword);

public record ChangePasswordRequestDto(string CurrentPassword, string NewPassword);

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    UserSummaryDto User);
