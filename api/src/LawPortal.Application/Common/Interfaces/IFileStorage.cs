namespace LawPortal.Application.Common.Interfaces;

public record PresignedUpload(string StorageKey, string UploadUrl, DateTime ExpiresAtUtc);

public interface IFileStorage
{
    /// <summary>A short-lived PUT URL the client uploads directly to — files never pass
    /// through the API process.</summary>
    PresignedUpload CreateUploadUrl(string fileName, string contentType);

    /// <summary>A short-lived GET URL, minted only after the caller's authorization check.</summary>
    string CreateDownloadUrl(string storageKey, TimeSpan ttl);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task<long> GetSizeAsync(string storageKey, CancellationToken cancellationToken = default);
}

public enum ScanOutcome
{
    Clean,
    Infected,
    ScanFailed,
}

public record ScanResult(ScanOutcome Outcome, string? Detail);

public interface IVirusScanner
{
    Task<ScanResult> ScanAsync(Stream content, CancellationToken cancellationToken = default);
}
