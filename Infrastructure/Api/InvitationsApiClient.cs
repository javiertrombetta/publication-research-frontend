using ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

namespace ResearchPublicationManagementSystem.Infrastructure.Api;

/// <summary>
/// Invitations. Sending, listing and withdrawing are an administrator's; previewing and
/// accepting are what the invited person does, and are anonymous — by definition they have no
/// account yet, so those two calls carry no bearer token.
/// </summary>
public class InvitationsApiClient(HttpClient httpClient) : ApiClientBase(httpClient)
{
    public Task<ApiResult<IReadOnlyList<UserInvitationDto>>> GetAllAsync(CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<UserInvitationDto>>("api/invitations", ct);

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
