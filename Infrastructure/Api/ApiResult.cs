namespace ResearchPublicationManagementSystem.Infrastructure.Api;

/// <summary>
/// Uniform result of a backend API call, hiding the two different failure shapes the backend
/// can return (see ApiClientBase): business-rule errors carry only ErrorMessage; FluentValidation's
/// unwrapped ValidationProblemDetails failures carry FieldErrors instead.
/// </summary>
public class ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyDictionary<string, string[]>? FieldErrors { get; init; }
    public int StatusCode { get; init; }

    /// <summary>
    /// The API could not be reached at all, or answered that it is not currently able to serve.
    /// Distinct from an ordinary failure: nothing the user did caused it and nothing they can do
    /// will fix it, so a screen should say the site is unavailable rather than show its own
    /// half-empty version of itself.
    ///
    /// Zero is what ApiClientBase records for a connection failure or a timeout. The three
    /// gateway statuses are what a platform's edge returns while the service behind it is
    /// restarting or has fallen over — which is exactly a deploy in progress.
    /// </summary>
    public bool IsServiceUnavailable =>
        !Success && StatusCode is 0 or 502 or 503 or 504;

    public static ApiResult<T> Ok(T? data, int statusCode = 200) =>
        new() { Success = true, Data = data, StatusCode = statusCode };

    public static ApiResult<T> Fail(string message, int statusCode, IReadOnlyDictionary<string, string[]>? fieldErrors = null) =>
        new() { Success = false, ErrorMessage = message, StatusCode = statusCode, FieldErrors = fieldErrors };
}
