using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

public class AuthApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<object?>> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>("api/auth/register", request, ct);

    public Task<ApiResult<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<AuthResponseDto>("api/auth/login", request, ct);

    public Task<ApiResult<AuthResponseDto>> RefreshAsync(string refreshToken, CancellationToken ct = default) =>
        PostJsonAsync<AuthResponseDto>("api/auth/refresh", new RefreshTokenRequestDto(refreshToken), ct);

    public Task<ApiResult<object?>> LogoutAsync(string refreshToken, CancellationToken ct = default) =>
        PostJsonAsync<object?>("api/auth/logout", new RefreshTokenRequestDto(refreshToken), ct);

    public Task<ApiResult<object?>> VerifyEmailAsync(Guid userId, string token, CancellationToken ct = default) =>
        GetAsync<object?>($"api/auth/verify-email?userId={userId}&token={Uri.EscapeDataString(token)}", ct);

    public Task<ApiResult<object?>> ForgotPasswordAsync(string email, CancellationToken ct = default) =>
        PostJsonAsync<object?>("api/auth/forgot-password", new ForgotPasswordRequestDto(email), ct);

    public Task<ApiResult<object?>> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>("api/auth/reset-password", request, ct);

    public Task<ApiResult<object?>> ChangePasswordAsync(ChangePasswordRequestDto request, string accessToken, CancellationToken ct = default) =>
        PostJsonAsync<object?>("api/auth/change-password", request, ct, accessToken);
}
