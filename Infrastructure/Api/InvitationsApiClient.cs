using Microsoft.AspNetCore.WebUtilities;
using ResearchPublicationManagementSystem.Common;
using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

/// <summary>
/// Invitations. Sending, listing and withdrawing are an administrator's; previewing and accepting
/// are what the invited person does, and are anonymous, because by definition they have no account
/// yet, so those two calls carry no bearer token.
/// </summary>
public class InvitationsApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    /// <summary>
    /// One page of invitations. <paramref name="state"/> is "Pending" or "Settled", which is how
    /// the screen's two blocks each get a page of their own instead of sharing one and each
    /// showing whatever part of it happened to belong to them.
    /// </summary>
    public Task<ApiResult<PagedResultDto<UserInvitationDto>>> GetAllAsync(
        string? state = null, int page = 1, string? search = null,
        string? sort = null, bool descending = false, CancellationToken ct = default)
    {
        var parameters = Page(page, Paging.AsConfigured);
        parameters["state"] = state;
        parameters["search"] = search;

        if (!string.IsNullOrWhiteSpace(sort))
        {
            parameters["sortBy"] = sort;
            parameters["sortDescending"] = descending ? "true" : "false";
        }

        return GetAsync<PagedResultDto<UserInvitationDto>>(
            QueryHelpers.AddQueryString("api/invitations",
                parameters.Where(p => !string.IsNullOrWhiteSpace(p.Value))), ct);
    }

    public Task<ApiResult<UserInvitationDto>> CreateAsync(
        CreateInvitationRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<UserInvitationDto>("api/invitations", request, ct);

    public Task<ApiResult<UserInvitationDto>> ResendAsync(Guid id, CancellationToken ct = default) =>
        PostAsync<UserInvitationDto>($"api/invitations/{id}/resend", ct);

    public Task<ApiResult<UserInvitationDto>> RevokeAsync(Guid id, CancellationToken ct = default) =>
        PostAsync<UserInvitationDto>($"api/invitations/{id}/revoke", ct);

    public Task<ApiResult<InvitationPreviewDto>> PreviewAsync(string token, CancellationToken ct = default) =>
        GetAsync<InvitationPreviewDto>($"api/invitations/preview?token={Uri.EscapeDataString(token)}", ct);

    public Task<ApiResult<object?>> AcceptAsync(AcceptInvitationRequestDto request, CancellationToken ct = default) =>
        PostJsonAsync<object?>("api/invitations/accept", request, ct);
}
