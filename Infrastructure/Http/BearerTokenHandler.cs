using System.Net;
using System.Net.Http.Headers;
using ResearchPublicationManagementSystem.Infrastructure.Api;
using ResearchPublicationManagementSystem.Services;

namespace ResearchPublicationManagementSystem.Infrastructure.Http;

/// <summary>
/// Attaches the signed-in user's JWT access token to every outgoing API request, and on a 401
/// response attempts exactly one silent refresh-and-retry before giving up. On refresh failure,
/// signs the user out and flags the request for ForceReauthFilter to redirect to login.
/// </summary>
public class BearerTokenHandler(IHttpContextAccessor httpContextAccessor, IAuthCookieService authCookieService) : DelegatingHandler
{
    public const string ForceReauthItemKey = "ForceReauth";

    private static readonly TimeSpan ProactiveRefreshWindow = TimeSpan.FromMinutes(2);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var accessToken = httpContext?.User.FindFirst(AuthClaimTypes.AccessToken)?.Value;

        // Refresh ahead of expiry when possible. Besides avoiding a failed-request round trip, this
        // matters for multipart uploads: a reactive 401-retry can't safely resend a stream-backed
        // request body that's already been read once.
        if (httpContext is not null && accessToken is not null && IsNearExpiry(httpContext))
        {
            var refreshToken = httpContext.User.FindFirst(AuthClaimTypes.RefreshToken)?.Value;
            if (refreshToken is not null)
            {
                var refreshed = await authCookieService.TryRefreshAsync(httpContext, refreshToken, cancellationToken);
                if (refreshed is not null) accessToken = refreshed;
            }
        }

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized || httpContext is null)
        {
            return response;
        }

        var refreshTokenForRetry = httpContext.User.FindFirst(AuthClaimTypes.RefreshToken)?.Value;
        if (refreshTokenForRetry is null)
        {
            return response;
        }

        var newAccessToken = await authCookieService.TryRefreshAsync(httpContext, refreshTokenForRetry, cancellationToken);
        if (newAccessToken is null)
        {
            await authCookieService.SignOutAsync(httpContext);
            httpContext.Items[ForceReauthItemKey] = true;
            return response;
        }

        // The refresh worked, but only replay the request if its body can actually be sent twice. A
        // multipart upload's content is backed by an IFormFile stream that this request already
        // consumed. Replaying it would silently send an empty body, which is worse than surfacing
        // the 401 as "session expired, please retry".
        if (!request.Options.TryGetValue(ApiClientBase.ReplayableOption, out var replayable) || !replayable)
        {
            return response;
        }

        response.Dispose();

        using var retryRequest = CloneRequest(request, newAccessToken);
        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private static bool IsNearExpiry(HttpContext httpContext)
    {
        var expiresAtRaw = httpContext.User.FindFirst(AuthClaimTypes.AccessTokenExpiresAt)?.Value;
        if (expiresAtRaw is null || !DateTime.TryParse(expiresAtRaw, out var expiresAt)) return false;
        return DateTime.UtcNow >= expiresAt.ToUniversalTime() - ProactiveRefreshWindow;
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage original, string newAccessToken)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Content = original.Content,
            Version = original.Version
        };
        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        clone.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
        return clone;
    }
}
