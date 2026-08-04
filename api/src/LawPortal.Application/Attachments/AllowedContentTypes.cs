namespace LawPortal.Application.Attachments;

/// <summary>The documented upload rule: "يدعم: الصور، الفيديو، PDF، Word، Excel، الملفات المضغوطة".</summary>
public static class AllowedContentTypes
{
    private static readonly HashSet<string> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif", "image/heic",
        "video/mp4", "video/quicktime", "video/webm",
        "application/pdf",
        "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/zip", "application/x-zip-compressed", "application/x-rar-compressed", "application/x-7z-compressed",
        // Voice notes recorded in-browser.
        "audio/webm", "audio/mp4", "audio/mpeg", "audio/aac",
    };

    public static bool IsAllowed(string contentType) => Types.Contains(contentType);
}
