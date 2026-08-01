namespace ResearchPublicationManagementSystem.Infrastructure.Http;

/// <summary>
/// Notices when the backend is not answering and records it on the current request, so <see
/// cref="ApiUnavailableFilter"/> can show the maintenance notice instead of letting each screen
/// render its own broken, half-empty version of itself.
///
/// It sits in the message pipeline rather than in ApiClientBase because that is the one place every
/// outgoing call passes through, whichever typed client made it, and because reaching for the
/// current request from inside the clients would mean threading an accessor through a dozen
/// constructors for something none of them care about.
///
/// The request is never failed here: the exception is re-thrown and the response returned
/// untouched, so the clients keep turning them into ordinary failed results. This only observes.
/// </summary>
public class ApiAvailabilityHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    public const string UnavailableItemKey = "RpmsApiUnavailable";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;

        try
        {
            response = await base.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Refused, unresolvable, TLS refused: the service is not there.
            MarkUnavailable();
            throw;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Its own timeout rather than the caller giving up. The service is there but not
            // answering, which for someone waiting on a page is the same thing.
            MarkUnavailable();
            throw;
        }

        // The platform's edge answering on behalf of a service that is restarting or has fallen
        // over. A deploy in progress looks exactly like this.
        if ((int)response.StatusCode is 502 or 503 or 504)
        {
            MarkUnavailable();
        }

        return response;
    }

    private void MarkUnavailable()
    {
        var items = httpContextAccessor.HttpContext?.Items;
        if (items is not null)
        {
            items[UnavailableItemKey] = true;
        }
    }
}
