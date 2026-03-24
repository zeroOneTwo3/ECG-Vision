namespace EcgVision.Core.Interfaces;

public interface IS3Client
{
    Task<string> UploadAsync(string key, Stream dataStream, string contentType);

    Task<Stream> DownloadAsync(string key);

    Task<(string Link, DateTimeOffset ExpiresAt)> GetPresignedUrlAsync(string key);
}