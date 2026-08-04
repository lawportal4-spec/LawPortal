namespace LawPortal.Application.Notifications;

public record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    Guid? RelatedThreadId,
    bool IsRead,
    DateTime CreatedAtUtc);
