namespace LawPortal.Application.Chat.Dtos;

public record MessageDto(Guid Id, Guid ThreadId, Guid SenderUserId, string Body, DateTime SentAtUtc);

public record ThreadSummaryDto(
    Guid Id,
    Guid ServiceRequestId,
    string RequestNumber,
    string OtherPartyName,
    string? LastMessageBody,
    DateTime? LastMessageAtUtc,
    int UnreadCount);

public record ThreadDetailDto(
    Guid Id,
    Guid ServiceRequestId,
    string RequestNumber,
    Guid OtherPartyUserId,
    string OtherPartyName,
    bool IsOtherPartyOnline,
    bool IsBlockedByMe,
    bool HasBlockedMe);
