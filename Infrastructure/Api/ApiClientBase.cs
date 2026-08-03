using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

/// <summary>
/// Shared plumbing for every typed API client: JSON (de)serialization, multipart upload, and
/// branching on the backend's two distinct failure shapes (see SendAsync).
/// </summary>
public abstract class ApiClientBase(HttpClient httpClient)
{

    /// <summary>
    /// Marks a request whose body can safely be sent a second time, so BearerTokenHandler knows it
    /// may replay it after a token refresh. JSON/no-body requests are replayable; multipart uploads
    /// are not. Their content is backed by a forward-only IFormFile stream that is already consumed
    /// by the time a 401 comes back.
    /// </summary>
    public static readonly HttpRequestOptionsKey<bool> ReplayableOption = new("RpmsReplayable");

    protected readonly HttpClient Http = httpClient;

    protected static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected Task<ApiResult<T>> GetAsync<T>(string url, CancellationToken ct = default) =>
        SendAsync<T>(Replayable(new HttpRequestMessage(HttpMethod.Get, url)), ct);

    protected Task<ApiResult<T>> PostJsonAsync<T>(string url, object? body, CancellationToken ct = default, string? bearerToken = null) =>
        SendAsync<T>(WithBearer(Replayable(new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonBody(body) }), bearerToken), ct);

    protected Task<ApiResult<T>> PutJsonAsync<T>(string url, object? body, CancellationToken ct = default) =>
        SendAsync<T>(Replayable(new HttpRequestMessage(HttpMethod.Put, url) { Content = JsonBody(body) }), ct);

    protected Task<ApiResult<T>> PostAsync<T>(string url, CancellationToken ct = default) =>
        SendAsync<T>(Replayable(new HttpRequestMessage(HttpMethod.Post, url)), ct);

    protected Task<ApiResult<T>> DeleteAsync<T>(string url, CancellationToken ct = default) =>
        SendAsync<T>(Replayable(new HttpRequestMessage(HttpMethod.Delete, url)), ct);

    /// <summary>
    /// A DELETE that carries a body. Unusual, but the API requires a reason with destructive
    /// actions so that the audit trail records why they happened.
    /// </summary>
    protected Task<ApiResult<T>> DeleteJsonAsync<T>(string url, object? body, CancellationToken ct = default) =>
        SendAsync<T>(Replayable(new HttpRequestMessage(HttpMethod.Delete, url) { Content = JsonBody(body) }), ct);

    /// <summary>
    /// Fetches a binary response (a stored file) rather than the JSON envelope. Returns null
    /// when the backend has nothing to serve, which callers treat as "no file".
    /// </summary>
    protected async Task<(byte[] Content, string ContentType)?> GetBytesAsync(string url, CancellationToken ct = default)
    {
        using var request = Replayable(new HttpRequestMessage(HttpMethod.Get, url));

        try
        {
            using var response = await Http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return (bytes, contentType);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <summary>
    /// Fetches a stored file along with the name the API gave it.
    ///
    /// Separate from GetBytesAsync because a download needs the name: the API composes one from
    /// the paper's title or the form's name, and the stored file is a GUID. Returns null when
    /// there is nothing to serve, which callers turn into a 404 rather than an empty download.
    /// </summary>
    protected async Task<(byte[] Content, string ContentType, string FileName)?> GetFileAsync(
        string url, CancellationToken ct = default)
    {
        using var request = Replayable(new HttpRequestMessage(HttpMethod.Get, url));

        try
        {
            using var response = await Http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                           ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                           ?? "download";

            return (bytes, contentType, fileName);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <summary>
    /// The API is not answering. Recording that on the request is ApiAvailabilityHandler's job. It
    /// sits in the message pipeline and sees every call, including the ones whose result never
    /// reaches a controller. This only shapes the result.
    /// </summary>
    private static ApiResult<T> Unreachable<T>(string message, int statusCode = 0) =>
        ApiResult<T>.Fail(message, statusCode);

    private static HttpRequestMessage Replayable(HttpRequestMessage request)
    {
        request.Options.Set(ReplayableOption, true);
        return request;
    }

    /// <summary>
    /// Used only by AuthApiClient, whose HttpClient deliberately carries no BearerTokenHandler
    /// (that handler depends on IAuthCookieService, which depends on AuthApiClient for token
    /// refresh, and attaching the handler here would create a DI cycle). Its one authenticated
    /// endpoint (change-password) attaches the token explicitly instead.
    /// </summary>
    private static HttpRequestMessage WithBearer(HttpRequestMessage request, string? bearerToken)
    {
        if (bearerToken is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        return request;
    }

    private static StringContent JsonBody(object? body) =>
        new(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json");

    /// <summary>
    /// Streams IFormFile content directly into a multipart request without buffering the whole file
    /// into memory, needed for paper-version uploads up to 200MB.
    /// </summary>
    protected async Task<ApiResult<T>> PostMultipartAsync<T>(
        string url,
        IEnumerable<(string Name, IFormFile? File)> files,
        IEnumerable<(string Name, string? Value)>? fields = null,
        CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        var streams = new List<Stream>();
        try
        {
            foreach (var (name, file) in files)
            {
                if (file is null) continue;
                var stream = file.OpenReadStream();
                streams.Add(stream);
                var part = new StreamContent(stream);
                part.Headers.ContentType = new MediaTypeHeaderValue(
                    string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType);
                content.Add(part, name, file.FileName);
            }

            if (fields is not null)
            {
                foreach (var (name, value) in fields)
                {
                    if (value is not null) content.Add(new StringContent(value), name);
                }
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            return await SendAsync<T>(request, ct);
        }
        finally
        {
            foreach (var s in streams) await s.DisposeAsync();
        }
    }

    /// <summary>
    /// Sends the request and normalizes the response. Success and business-rule-error responses
    /// share the {success,data,message,errors} envelope; FluentValidation's automatic model-binding
    /// failures instead return the unwrapped ASP.NET ValidationProblemDetails shape
    /// ({type,title,status,errors:{field:[msg]}}), so we branch on whether a top-level "success"
    /// property is present before picking how to parse the body.
    /// </summary>
    private async Task<ApiResult<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken ct)
    {
        HttpResponseMessage response;
        string body;
        try
        {
            response = await Http.SendAsync(request, ct);
            body = await response.Content.ReadAsStringAsync(ct);
        }
        catch (HttpRequestException ex)
        {
            return Unreachable<T>($"Could not reach the server: {ex.Message}");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return Unreachable<T>("The server did not respond in time.");
        }

        using (response)
        {
            var statusCode = (int)response.StatusCode;

            // The edge answering for a service that is restarting or has fallen over. Treated the
            // same as not reaching it at all, because to the person looking at the screen it is.
            if (statusCode is 502 or 503 or 504)
            {
                return Unreachable<T>("The server is not available at the moment.", statusCode);
            }

            // A 401 that survived BearerTokenHandler means the token could not be refreshed, or
            // the request body could not be replayed after refreshing (multipart uploads).
            // The framework's bare 401 carries no body, so give the caller something actionable.
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return ApiResult<T>.Fail("Your session expired. Please sign in again and retry.", statusCode);
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                return response.IsSuccessStatusCode
                    ? ApiResult<T>.Ok(default, statusCode)
                    : ApiResult<T>.Fail($"Request failed ({statusCode}).", statusCode);
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(body);
            }
            catch (JsonException)
            {
                return ApiResult<T>.Fail($"Unexpected response ({statusCode}).", statusCode);
            }

            using (doc)
            {
                if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("success", out var successEl))
                {
                    var envelopeSuccess = successEl.GetBoolean();
                    var message = doc.RootElement.TryGetProperty("message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String
                        ? msgEl.GetString()
                        : null;

                    if (envelopeSuccess)
                    {
                        var data = doc.RootElement.TryGetProperty("data", out var dataEl) && dataEl.ValueKind != JsonValueKind.Null
                            ? dataEl.Deserialize<T>(JsonOpts)
                            : default;
                        return ApiResult<T>.Ok(data, statusCode);
                    }

                    // Errors is a flat string[] here (not field-keyed), so surface it as one
                    // combined message.
                    var errorList = doc.RootElement.TryGetProperty("errors", out var errorsEl) && errorsEl.ValueKind == JsonValueKind.Array
                        ? errorsEl.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToArray()
                        : [];
                    var combined = errorList.Length > 0
                        ? string.Join(" ", errorList)
                        : message ?? "Request failed.";
                    return ApiResult<T>.Fail(combined, statusCode);
                }

                if (statusCode == (int)HttpStatusCode.BadRequest && doc.RootElement.TryGetProperty("errors", out var fieldErrorsEl)
                    && fieldErrorsEl.ValueKind == JsonValueKind.Object)
                {
                    var fieldErrors = fieldErrorsEl.Deserialize<Dictionary<string, string[]>>(JsonOpts) ?? new();

                    // What was actually wrong, not that something was. The title of this shape is
                    // always "One or more validation errors occurred", which is what a screen with
                    // nowhere to put field errors ended up showing somebody: true, and no help at
                    // all. Where the reasons are readable, they are the message.
                    var reasons = fieldErrors.SelectMany(pair => pair.Value)
                        .Where(reason => !string.IsNullOrWhiteSpace(reason))
                        .ToArray();

                    var title = doc.RootElement.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : null;
                    var message = reasons.Length > 0
                        ? string.Join(" ", reasons)
                        : title ?? "Validation failed.";

                    return ApiResult<T>.Fail(message, statusCode, fieldErrors);
                }

                return ApiResult<T>.Fail($"Unexpected response ({statusCode}).", statusCode);
            }
        }
    }
}
