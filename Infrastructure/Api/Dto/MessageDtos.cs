namespace ResearchPublicationManagementSystem.Infrastructure.Api.Dto;

/// <summary>Somebody the signed-in person may write to about a publication, and why.</summary>
public record MessageCounterpartDto(
    Guid UserId, string Name, string Role, int UnreadFromThem, DateTime? LastMessageAt);

/// <param name="Outgoing">True when the signed-in person wrote it.</param>
public record ContainerMessageDto(
    Guid Id,
    Guid SenderUserId,
    string SenderName,
    Guid RecipientUserId,
    string RecipientName,
    string Body,
    DateTime SentAt,
    bool Outgoing,
    bool ReadByRecipient,
    IReadOnlyList<MessageAttachmentDto> Attachments);

public record MessageAttachmentDto(Guid Id, string FileName, long SizeInBytes);

/// <summary>What the screen needs before anybody has written anything.</summary>
public record ContainerMessagingDto(
    bool Enabled,
    IReadOnlyList<MessageCounterpartDto> Counterparts,
    string AllowedExtensions,
    int MaximumLength,
    int MaximumAttachments);
