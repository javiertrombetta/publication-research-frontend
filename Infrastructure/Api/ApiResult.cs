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

    public static ApiResult<T> Ok(T? data, int statusCode = 200) =>
        new() { Success = true, Data = data, StatusCode = statusCode };

    public static ApiResult<T> Fail(string message, int statusCode, IReadOnlyDictionary<string, string[]>? fieldErrors = null) =>
        new() { Success = false, ErrorMessage = message, StatusCode = statusCode, FieldErrors = fieldErrors };
}
