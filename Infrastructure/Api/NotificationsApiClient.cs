using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

/// <summary>
/// The signed-in person's own notifications. Every role has them, so this is not scoped to one.
/// </summary>
public class NotificationsApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<IReadOnlyList<NotificationDto>>> GetAsync(bool unreadOnly = false, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<NotificationDto>>(
            unreadOnly ? "api/notifications?unreadOnly=true" : "api/notifications", ct);

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
