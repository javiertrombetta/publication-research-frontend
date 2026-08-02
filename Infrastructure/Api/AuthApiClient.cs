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

    /// <summary>
    /// Trades a token Microsoft Entra issued for this application's own. The caller has proved who
    /// they are to the institution; this asks the API who that is here.
    ///
    /// The token goes in as the bearer because that is what the endpoint authenticates against: it
    /// is guarded by the Entra scheme rather than by ours. Where no tenant is configured on the API
    /// the answer is a plain 401 saying so.
    /// </summary>
    public Task<ApiResult<AuthResponseDto>> AzureSsoExchangeAsync(string entraAccessToken, CancellationToken ct = default) =>
        PostJsonAsync<AuthResponseDto>("api/auth/azure-sso/exchange", new { }, ct, entraAccessToken);
}
