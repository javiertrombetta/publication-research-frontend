using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

/// <summary>
/// What people have written to each other about one publication. Every call is scoped to the
/// signed-in person by the API: access to the publication does not open somebody else's
/// conversation.
/// </summary>
public class ContainerMessagesApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    /// <summary>Whether this is on, who may be written to, and what a message may carry.</summary>
    public Task<ApiResult<ContainerMessagingDto>> GetContextAsync(Guid containerId, CancellationToken ct = default) =>
        GetAsync<ContainerMessagingDto>($"api/containers/{containerId}/messages/context", ct);

    public Task<ApiResult<PagedResultDto<ContainerMessageDto>>> GetMessagesAsync(
        Guid containerId, Guid? with = null, int page = 1, CancellationToken ct = default)
    {
        var query = new List<string>();
        if (with is { } other) query.Add("with=" + other);
        if (page > 1) query.Add("page=" + page);

        var url = $"api/containers/{containerId}/messages" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
        return GetAsync<PagedResultDto<ContainerMessageDto>>(url, ct);
    }

    public Task<ApiResult<ContainerMessageDto>> SendAsync(
        Guid containerId, Guid recipientUserId, string body, IReadOnlyList<IFormFile>? files, CancellationToken ct = default) =>
        PostMultipartAsync<ContainerMessageDto>(
            $"api/containers/{containerId}/messages",
            files?.Select(f => ("Files", (IFormFile?)f)) ?? [],
            [("RecipientUserId", recipientUserId.ToString()), ("Body", body)],
            ct);

    public Task<ApiResult<object?>> MarkReadAsync(Guid containerId, Guid with, CancellationToken ct = default) =>
        PutJsonAsync<object?>($"api/containers/{containerId}/messages/read?with={with}", null, ct);

    public Task<(byte[] Content, string ContentType, string FileName)?> DownloadAttachmentAsync(
        Guid containerId, Guid attachmentId, CancellationToken ct = default) =>
        GetFileAsync($"api/containers/{containerId}/messages/attachments/{attachmentId}", ct);

    // ---------- What an administrator has decided about this publication ----------

    public Task<ApiResult<ContainerMessagingRulesDto>> GetRulesAsync(Guid containerId, CancellationToken ct = default) =>
        GetAsync<ContainerMessagingRulesDto>($"api/containers/{containerId}/messages/rules", ct);

    public Task<ApiResult<ContainerMessagingRuleDto>> SetRuleAsync(
        Guid containerId, SetContainerMessagingRuleRequestDto request, CancellationToken ct = default) =>
        PutJsonAsync<ContainerMessagingRuleDto>($"api/containers/{containerId}/messages/rules", request, ct);

    public Task<ApiResult<object?>> RemoveRuleAsync(Guid containerId, Guid ruleId, CancellationToken ct = default) =>
        DeleteAsync<object?>($"api/containers/{containerId}/messages/rules/{ruleId}", ct);
}
