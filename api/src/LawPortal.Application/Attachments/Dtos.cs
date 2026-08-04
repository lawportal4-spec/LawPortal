namespace LawPortal.Application.Attachments;

public record PresignedUploadDto(string StorageKey, string UploadUrl, DateTime ExpiresAtUtc);

public record ConfirmedAttachmentDto(Guid Id, string ScanStatus);
