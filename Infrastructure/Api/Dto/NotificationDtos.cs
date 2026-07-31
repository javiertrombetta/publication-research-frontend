namespace ResearchPublicationManagementSystem.Infrastructure.Api.Dto
{
    /// <summary>
    /// One notification. <see cref="RelatedEntityType"/> and <see cref="RelatedEntityId"/> say
    /// what it is about, which is what lets a notification link somewhere rather than merely
    /// announce something.
    /// </summary>
    public record NotificationDto(
        Guid Id,
        string Type,
        string Title,
        string Message,
        string? RelatedEntityType,
        Guid? RelatedEntityId,
        bool IsRead,
        DateTime CreatedAt);
}
