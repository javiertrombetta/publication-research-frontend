using Microsoft.AspNetCore.WebUtilities;
using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

public class ContainersApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<PublicationContainerDto>> CreateAsync(CancellationToken ct = default) =>
        PostAsync<PublicationContainerDto>("api/containers", ct);

    /// <summary>One page of the acting student's publications, newest first.</summary>
    public Task<ApiResult<PagedResultDto<PublicationContainerDto>>> GetMineAsync(
        int page = 1, int pageSize = Paging.DefaultPageSize, CancellationToken ct = default) =>
        GetAsync<PagedResultDto<PublicationContainerDto>>(
            QueryHelpers.AddQueryString("api/containers/me", Page(page, pageSize)), ct);

    public Task<ApiResult<PublicationContainerDto>> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<PublicationContainerDto>($"api/containers/{id}", ct);

    /// <summary>Discards one of the student's own publications; the backend rejects it once it holds proposals.</summary>
    public Task<ApiResult<object?>> DeleteAsync(Guid id, CancellationToken ct = default) =>
        DeleteAsync<object?>($"api/containers/{id}", ct);

    public Task<ApiResult<IReadOnlyList<ActivityHistoryEntryDto>>> GetActivityHistoryAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<ActivityHistoryEntryDto>>($"api/containers/{id}/activity-history", ct);

    /// <summary>
    /// Containers filtered server-side. A Coordinator passes their own id so the listing is
    /// their workload rather than the whole institution's.
    /// </summary>
    public Task<ApiResult<PagedResultDto<PublicationContainerDto>>> GetAllAsync(
        Guid? studentId = null, Guid? coordinatorId = null, string? status = null,
        string? ethicsSteps = null, int page = 1, int pageSize = Paging.DefaultPageSize,
        string? sort = null, bool descending = false, string? search = null,
        string? paperAwaiting = null, CancellationToken ct = default)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["studentId"] = studentId?.ToString(),
            ["coordinatorId"] = coordinatorId?.ToString(),
            ["status"] = status,
            // Which ethics decision the screen is about. Sent so the API returns that screen's
            // queue rather than everything, which is what makes a page of it a stable page.
            ["ethicsSteps"] = ethicsSteps,
            // One term across the student, the title and the abstract, applied by the API so it
            // covers the whole queue rather than the page already in hand.
            ["search"] = search,
            // Whose turn it is on the paper, so a screen showing two lists can ask for each of
            // them separately and page both.
            ["paperAwaiting"] = paperAwaiting
        };

        foreach (var (key, value) in Page(page, pageSize)) parameters[key] = value;

        // Left off when nothing is chosen, so the endpoint keeps its own default order.
        if (!string.IsNullOrWhiteSpace(sort))
        {
            parameters["sortBy"] = sort;
            parameters["sortDescending"] = descending.ToString().ToLowerInvariant();
        }

        var url = QueryHelpers.AddQueryString("api/containers",
            parameters.Where(p => !string.IsNullOrWhiteSpace(p.Value)));

        return GetAsync<PagedResultDto<PublicationContainerDto>>(url, ct);
    }

    /// <summary>The page parameters every listing here carries.</summary>
    private static Dictionary<string, string?> Page(int page, int pageSize) => new()
    {
        ["page"] = Math.Max(1, page).ToString(),
        ["pageSize"] = pageSize.ToString()
    };

    /// <summary>The publications this supervisor has been assigned to, newest first.</summary>
    public Task<ApiResult<PagedResultDto<PublicationContainerDto>>> GetSupervisingAsync(
        string? ethicsSteps = null, int page = 1, int pageSize = Paging.DefaultPageSize,
        string? sort = null, bool descending = false, string? search = null,
        CancellationToken ct = default) =>
        GetAsync<PagedResultDto<PublicationContainerDto>>(
            WithSteps("api/containers/supervising", ethicsSteps, page, pageSize, sort, descending, search), ct);

    /// <summary>Publications by students in this Head of Department's department.</summary>
    public Task<ApiResult<PagedResultDto<PublicationContainerDto>>> GetInMyDepartmentAsync(
        string? ethicsSteps = null, int page = 1, int pageSize = Paging.DefaultPageSize,
        string? sort = null, bool descending = false, string? search = null,
        string? paperAwaiting = null, CancellationToken ct = default)
    {
        var path = WithSteps("api/containers/in-my-department", ethicsSteps, page, pageSize, sort, descending, search);

        // Whose turn it is on the paper, so the head of department can look at the research paper
        // stage on its own rather than reading it out of the whole department's listing.
        return GetAsync<PagedResultDto<PublicationContainerDto>>(
            string.IsNullOrWhiteSpace(paperAwaiting)
                ? path
                : path + (path.Contains('?') ? "&" : "?") + $"paperAwaiting={Uri.EscapeDataString(paperAwaiting)}",
            ct);
    }

    private static string WithSteps(string path, string? ethicsSteps, int page, int pageSize,
        string? sort = null, bool descending = false, string? search = null)
    {
        var parameters = Page(page, pageSize);
        parameters["ethicsSteps"] = ethicsSteps;

        // One term across the student, the title and the abstract, applied by the API so it covers
        // the whole queue rather than the page already in hand.
        parameters["search"] = search;

        // Left off entirely when nothing is chosen, so the endpoint applies its own default order
        // rather than being told to sort by an empty column.
        if (!string.IsNullOrWhiteSpace(sort))
        {
            parameters["sortBy"] = sort;
            parameters["sortDescending"] = descending.ToString().ToLowerInvariant();
        }

        return QueryHelpers.AddQueryString(path, parameters.Where(p => !string.IsNullOrWhiteSpace(p.Value)));
    }
}
