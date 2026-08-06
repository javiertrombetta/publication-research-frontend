using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

/// <summary>
/// The signed-in person's own notifications. Every role has them, so this is not scoped to one.
/// </summary>
public class NotificationsApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    /// <summary>
    /// One page of them, newest first, optionally only the unread ones and optionally narrowed by
    /// a search. Page size is left to the API, which fills in whatever the institution configured.
    /// </summary>
    public Task<ApiResult<PagedResultDto<NotificationDto>>> GetAsync(
        bool unreadOnly = false, string? search = null, int page = 1, CancellationToken ct = default)
    {
        var query = new List<string>();
        if (unreadOnly) query.Add("unreadOnly=true");
        if (!string.IsNullOrWhiteSpace(search)) query.Add("search=" + Uri.EscapeDataString(search.Trim()));
        if (page > 1) query.Add("page=" + page);

        var url = "api/notifications" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
        return GetAsync<PagedResultDto<NotificationDto>>(url, ct);
    }

    /// <summary>
    /// One of them, by id, for opening it. Its own request rather than hunting through a page:
    /// the notification being opened is as likely to be on page four as page one.
    /// </summary>
    public Task<ApiResult<NotificationDto>> GetOneAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<NotificationDto>($"api/notifications/{id}", ct);

    /// <summary>
    /// Just the count, for the top bar. Its own endpoint rather than measuring a full listing:
    /// this is asked on every page load and does not need the notifications themselves.
    /// </summary>
    public Task<ApiResult<int>> GetUnreadCountAsync(CancellationToken ct = default) =>
        GetAsync<int>("api/notifications/unread-count", ct);

    public Task<ApiResult<object?>> MarkAsReadAsync(Guid id, CancellationToken ct = default) =>
        PutJsonAsync<object?>($"api/notifications/{id}/read", null, ct);

    public Task<ApiResult<object?>> MarkAllAsReadAsync(CancellationToken ct = default) =>
        PutJsonAsync<object?>("api/notifications/read", null, ct);
}
