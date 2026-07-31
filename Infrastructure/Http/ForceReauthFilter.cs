using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ResearchPublicationManagementSystem.Infrastructure.Http;

/// <summary>
/// Catches the flag BearerTokenHandler sets when a background token refresh fails mid-request
/// (refresh token itself expired/invalid) and redirects to login instead of letting the action's
/// own result render with stale/incomplete data.
/// </summary>
public class ForceReauthFilter : IAsyncResultFilter
{
    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.HttpContext.Items.ContainsKey(BearerTokenHandler.ForceReauthItemKey))
        {
            context.Result = new RedirectToActionResult("home", "Auth", new { returnUrl = context.HttpContext.Request.Path.Value });
            return Task.CompletedTask;
        }

        return next();
    }
}
