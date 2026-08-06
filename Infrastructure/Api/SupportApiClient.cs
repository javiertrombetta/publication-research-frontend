using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

/// <summary>
/// Writing to the institution's IT desk. Signed in only: the desk supports the institution's own
/// students and staff, and a form open to the world that emails files to a fixed address is a
/// relay for whoever finds it.
/// </summary>
public class SupportApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<SupportContactOptionsDto>> GetContactOptionsAsync(CancellationToken ct = default) =>
        GetAsync<SupportContactOptionsDto>("api/support/contact", ct);

    public Task<ApiResult<object?>> ContactAsync(
        string subject, string body, IReadOnlyList<IFormFile>? files, CancellationToken ct = default) =>
        PostMultipartAsync<object?>(
            "api/support/contact",
            files?.Select(f => ("Files", (IFormFile?)f)) ?? [],
            [("Subject", subject), ("Body", body)],
            ct);
}
