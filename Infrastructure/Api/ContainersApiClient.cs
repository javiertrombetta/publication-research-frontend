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
        int page = 1, int pageSize = Paging.AsConfigured, CancellationToken ct = default) =>
        GetAsync<PagedResultDto<PublicationContainerDto>>(
            QueryHelpers.AddQueryString("api/containers/me",
                Page(page, pageSize).Where(p => !string.IsNullOrWhiteSpace(p.Value))), ct);

    public Task<ApiResult<PublicationContainerDto>> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<PublicationContainerDto>($"api/containers/{id}", ct);

    /// <summary>Discards one of the student's own publications; the backend rejects it once it holds proposals.</summary>
    public Task<ApiResult<object?>> DeleteAsync(Guid id, CancellationToken ct = default) =>
        DeleteAsync<object?>($"api/containers/{id}", ct);

    /// <param name="from">Inclusive, as a date. <paramref name="to"/> is inclusive too.</param>
    /// <param name="action">One of the action names the trail records.</param>
    /// <param name="actorUserId">Whoever did it, or had it done on their behalf.</param>
    public Task<ApiResult<PagedResultDto<ActivityHistoryEntryDto>>> GetActivityHistoryAsync(
        Guid id, int page = 1, int pageSize = Paging.AsConfigured,
        DateOnly? from = null, DateOnly? to = null, string? action = null, Guid? actorUserId = null,
        CancellationToken ct = default)
    {
        var query = $"api/containers/{id}/activity-history?page={Math.Max(1, page)}{Paging.SizeParam(pageSize)}";

        if (from is { } f) query += $"&from={f:yyyy-MM-dd}";
        if (to is { } t) query += $"&to={t:yyyy-MM-dd}";
        if (!string.IsNullOrWhiteSpace(action)) query += $"&action={Uri.EscapeDataString(action)}";
        if (actorUserId is { } who) query += $"&actorUserId={who}";

        return GetAsync<PagedResultDto<ActivityHistoryEntryDto>>(query, ct);
    }

    /// <summary>
    /// Changes who is responsible for a publication already under way. Administrators only, and
    /// always with a reason.
    /// </summary>
    /// <summary>
    /// Admin-only: sets which step of which stage a publication waits at, so whoever should act
    /// next actually sees it.
    /// </summary>
    /// <summary>Every publication whose paper is at the status named.</summary>
    public Task<ApiResult<PagedResultDto<PublicationContainerDto>>> GetByPaperStatusAsync(
        string paperStatus, int page = 1, string? search = null,
        string? sort = null, bool descending = false, CancellationToken ct = default)
    {
        var parameters = Page(page, Paging.AsConfigured);
        parameters["paperStatus"] = paperStatus;
        parameters["search"] = search;

        if (!string.IsNullOrWhiteSpace(sort))
        {
            parameters["sortBy"] = sort;
            parameters["sortDescending"] = descending ? "true" : "false";
        }

        return GetAsync<PagedResultDto<PublicationContainerDto>>(
            QueryHelpers.AddQueryString("api/containers",
                parameters.Where(p => !string.IsNullOrWhiteSpace(p.Value))), ct);
    }

    /// <summary>
    /// One page of the publications a figure on the administrator's dashboard counts.
    ///
    /// Every one of those counts is a number with nothing behind it, and each of these narrows the
    /// same listing to exactly what it counted, so the figure becomes a way in rather than a
    /// statement. Whichever filter is set applies; passing none is the whole institution.
    /// </summary>
    public Task<ApiResult<PagedResultDto<PublicationContainerDto>>> GetByTallyAsync(
        string? status = null, string? pipeline = null, string? paperStatus = null,
        string? ethicsStatus = null, string? committeeDecision = null, string? reviewDecision = null,
        int page = 1, string? search = null, string? sort = null, bool descending = false,
        CancellationToken ct = default)
    {
        var parameters = Page(page, Paging.AsConfigured);
        parameters["status"] = status;
        parameters["pipeline"] = pipeline;
        parameters["paperStatus"] = paperStatus;
        parameters["ethicsStatus"] = ethicsStatus;
        parameters["committeeDecision"] = committeeDecision;
        parameters["reviewDecision"] = reviewDecision;
        parameters["search"] = search;

        if (!string.IsNullOrWhiteSpace(sort))
        {
            parameters["sortBy"] = sort;
            parameters["sortDescending"] = descending ? "true" : "false";
        }

        return GetAsync<PagedResultDto<PublicationContainerDto>>(
            QueryHelpers.AddQueryString("api/containers",
                parameters.Where(p => !string.IsNullOrWhiteSpace(p.Value))), ct);
    }

    public Task<ApiResult<PublicationContainerDto>> MoveAsync(
        Guid id, MoveContainerRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<PublicationContainerDto>($"api/containers/{id}/position", request, ct);

    public Task<ApiResult<PublicationContainerDto>> ReassignAsync(
        Guid id, ReassignContainerRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<PublicationContainerDto>($"api/containers/{id}/assignments", request, ct);

    /// <summary>What this publication's own trail can be filtered by.</summary>
    public Task<ApiResult<ActivityHistoryFiltersDto>> GetActivityHistoryFiltersAsync(
        Guid id, CancellationToken ct = default) =>
        GetAsync<ActivityHistoryFiltersDto>($"api/containers/{id}/activity-history/filters", ct);

    /// <summary>
    /// Containers filtered server-side. A Coordinator passes their own id so the listing is
    /// their workload rather than the whole institution's.
    /// </summary>
    public Task<ApiResult<PagedResultDto<PublicationContainerDto>>> GetAllAsync(
        Guid? studentId = null, Guid? coordinatorId = null, string? status = null,
        string? ethicsSteps = null, int page = 1, int pageSize = Paging.AsConfigured,
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

    /// <summary>The publications this supervisor has been assigned to, newest first.</summary>
    public Task<ApiResult<PagedResultDto<PublicationContainerDto>>> GetSupervisingAsync(
        string? ethicsSteps = null, int page = 1, int pageSize = Paging.AsConfigured,
        string? sort = null, bool descending = false, string? search = null,
        CancellationToken ct = default) =>
        GetAsync<PagedResultDto<PublicationContainerDto>>(
            WithSteps("api/containers/supervising", ethicsSteps, page, pageSize, sort, descending, search), ct);

    /// <summary>Publications by students in this Head of Department's department.</summary>
    public Task<ApiResult<PagedResultDto<PublicationContainerDto>>> GetInMyDepartmentAsync(
        string? ethicsSteps = null, int page = 1, int pageSize = Paging.AsConfigured,
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
