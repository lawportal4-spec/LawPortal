using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using LawPortal.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LawPortal.Infrastructure.Storage;

/// <summary>S3-compatible storage — talks to MinIO locally, swaps to real AWS S3 (or any other
/// S3-compatible provider) in production by changing configuration only (ServiceURL,
/// ForcePathStyle, and Region are the only provider-specific bits). Never serves attachments
/// directly: every read is a short-lived signed URL minted after the caller's own authorization
/// check.</summary>
public class S3FileStorage : IFileStorage
{
    private readonly AmazonS3Client _client;
    private readonly string _bucket;
    private readonly bool _endpointIsHttp;

    public S3FileStorage(IConfiguration configuration)
    {
        var section = configuration.GetSection("Storage");
        _bucket = section["Bucket"] ?? throw new InvalidOperationException("Storage:Bucket is not configured.");

        var endpoint = section["Endpoint"] ?? throw new InvalidOperationException("Storage:Endpoint is not configured.");
        _endpointIsHttp = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            // MinIO needs path-style (bucket-in-path); Railway's native buckets and most other
            // providers use virtual-hosted-style (bucket-in-host) — default true to preserve the
            // existing local/MinIO behavior, override with Storage:ForcePathStyle=false for those.
            ForcePathStyle = section.GetValue("ForcePathStyle", true),
            AuthenticationRegion = section["Region"] ?? "us-east-1",
            UseHttp = _endpointIsHttp,
            // AWSSDK.S3 3.7.412+ adds integrity checksums to every request by default, sent as
            // "Content-Encoding: aws-chunked" — which S3-compatible stores outside AWS (Railway's
            // Tigris buckets) reject with "The Content-Encoding HTTP header is invalid". Only
            // compute/validate them when the operation actually requires it.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };
        _client = new AmazonS3Client(section["AccessKey"], section["SecretKey"], config);
    }

    public PresignedUpload CreateUploadUrl(string fileName, string contentType)
    {
        var storageKey = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}/{fileName}";
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(15);

        var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = storageKey,
            Verb = HttpVerb.PUT,
            Expires = expiresAtUtc,
            ContentType = contentType,
        });

        return new PresignedUpload(storageKey, EnforceConfiguredScheme(url), expiresAtUtc);
    }

    public string CreateDownloadUrl(string storageKey, TimeSpan ttl)
    {
        var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = storageKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(ttl),
        });
        return EnforceConfiguredScheme(url);
    }

    /// <summary>The SDK sometimes mints presigned URLs on `https://` regardless of
    /// `ServiceURL`'s own scheme (and regardless of `UseHttp`) — fatal against a plain-HTTP
    /// local MinIO. Since the correct scheme is exactly what this class was configured with,
    /// rewrite it rather than fight the SDK's endpoint-resolution internals.</summary>
    private string EnforceConfiguredScheme(string url)
    {
        if (_endpointIsHttp && url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return "http://" + url["https://".Length..];
        return url;
    }

    public async Task<string> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken = default)
    {
        var storageKey = $"{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}/{fileName}";
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = storageKey,
            InputStream = content,
            ContentType = contentType,
            // Plain signed body instead of streaming "aws-chunked" signing, which Tigris also
            // rejects. Uploads here are small (licence ≤ 3MB, photo ≤ 2MB), so one body is fine.
            UseChunkEncoding = false,
        }, cancellationToken);
        return storageKey;
    }

    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetObjectAsync(_bucket, storageKey, cancellationToken);
        return response.ResponseStream;
    }

    public async Task<long> GetSizeAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetObjectMetadataAsync(_bucket, storageKey, cancellationToken);
        return response.ContentLength;
    }
}
