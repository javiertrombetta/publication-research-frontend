using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace ResearchPublicationManagementSystem.Infrastructure.Http;

/// <summary>
/// Replaces the page with a maintenance notice when the backend could not be reached while building
/// it.
///
/// Without this each screen renders its own idea of failure: an empty table here, a "we couldn't
/// load this" card there, a dashboard of zeroes, which reads as the application being broken rather
/// than temporarily unavailable. One honest message is better than a dozen half-truths.
///
/// Runs after the action, not before: whether the API answers is only known once the attempt has
/// been made.
/// </summary>
public class ApiUnavailableFilter : IAsyncResultFilter
{
    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (!context.HttpContext.Items.ContainsKey(ApiAvailabilityHandler.UnavailableItemKey))
        {
            return next();
        }

        // A redirect is left alone. An action that decided to send the person elsewhere already
        // handled its own failure, and overriding that would strand them on this page instead.
        if (context.Result is RedirectResult or RedirectToActionResult or RedirectToRouteResult)
        {
            return next();
        }

        // 503 rather than 200: this is the truth for a monitor, a crawler, or a browser cache,
        // and Retry-After keeps a crawler from hammering a service that is already struggling.
        context.HttpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.HttpContext.Response.Headers.RetryAfter = "60";

        // Whatever the action left to say about the failure describes this same outage, in the
        // words of a stack trace: "Connection refused (localhost:5020)" and the like. The page
        // already says it properly, and a toast quoting the transport on top of it both leaks
        // internals and contradicts the calm of the message.
        DiscardMessagesAboutTheOutage(context);

        context.Result = new ViewResult
        {
            ViewName = "Maintenance",

            // A fresh dictionary rather than the action's: its ModelState holds the same
            // complaint, and _Toasts renders anything left in there.
            ViewData = new ViewDataDictionary(
                new EmptyModelMetadataProvider(), new ModelStateDictionary())
        };

        return next();
    }

    private static void DiscardMessagesAboutTheOutage(ResultExecutingContext context)
    {
        context.ModelState.Clear();

        var tempData = context.HttpContext.RequestServices
            .GetService<ITempDataDictionaryFactory>()
            ?.GetTempData(context.HttpContext);

        if (tempData is null) return;

        tempData.Remove("ErrorMessage");
        tempData.Remove("SuccessMessage");
        tempData.Remove("InfoMessage");
    }
}
