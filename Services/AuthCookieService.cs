using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Services;

public class AuthCookieService(AuthApiClient authApiClient) : IAuthCookieService
{
    public async Task SignInAsync(HttpContext httpContext, AuthResponseDto auth, bool isPersistent = true)
    {
        var principal = BuildPrincipal(auth);
        var properties = new AuthenticationProperties { IsPersistent = isPersistent };
        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }

    public async Task SignOutAsync(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public async Task<string?> TryRefreshAsync(HttpContext httpContext, string refreshToken, CancellationToken ct = default)
    {
        var result = await authApiClient.RefreshAsync(refreshToken, ct);
        if (!result.Success || result.Data is null) return null;

        var principal = BuildPrincipal(result.Data);
        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        return result.Data.AccessToken;
    }

    public async Task SetProfilePhotoFlagAsync(HttpContext httpContext, bool hasPhoto)
    {
        // Carry every existing claim over untouched (tokens included) and only swap the flag,
        // so updating the avatar never disturbs the session.
        var claims = httpContext.User.Claims
            .Where(c => c.Type != AuthClaimTypes.HasProfilePhoto)
            .Append(new Claim(AuthClaimTypes.HasProfilePhoto, hasPhoto ? "true" : "false"))
            .ToList();

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    private static ClaimsPrincipal BuildPrincipal(AuthResponseDto auth)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.User.Id.ToString()),
            new(ClaimTypes.Email, auth.User.Email),
            new(ClaimTypes.GivenName, auth.User.FirstName),
            new(ClaimTypes.Surname, auth.User.LastName),
            new(AuthClaimTypes.AccessToken, auth.AccessToken),
            new(AuthClaimTypes.RefreshToken, auth.RefreshToken),
            new(AuthClaimTypes.AccessTokenExpiresAt, auth.AccessTokenExpiresAt.ToString("o")),
            new(AuthClaimTypes.HasProfilePhoto, auth.User.HasProfilePhoto ? "true" : "false")
        };

        claims.AddRange(auth.User.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
