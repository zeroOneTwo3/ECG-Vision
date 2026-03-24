using Amazon.S3;
using Amazon.S3.Model;

using EcgVision.Core.Interfaces;
using EcgVision.Infrastructure.Configuration;

using Microsoft.Extensions.Options;

namespace EcgVision.Infrastructure.ExternalServices;

public class S3Client : IS3Client
{
    private readonly IAmazonS3 _s3Client;
    private readonly IOptionsMonitor<S3Options> _options;
    private string BucketName => _options.CurrentValue.BucketName;

    public S3Client(IAmazonS3 s3Client, IOptionsMonitor<S3Options> options) =>
        (_s3Client, _options) = (s3Client, options);

    public async Task<string> UploadAsync(string key, Stream dataStream, string contentType)
    {
        var request = new PutObjectRequest
        {
            BucketName = BucketName,
            Key = key,
            InputStream = dataStream,
            ContentType = contentType,
            DisablePayloadSigning = true
        };

        await _s3Client.PutObjectAsync(request);
        return key;
    }

    public async Task<Stream> DownloadAsync(string key)
    {
        var request = new GetObjectRequest
        {
            BucketName = BucketName,
            Key = key,
        };

        var response = await _s3Client.GetObjectAsync(request);
        return response.ResponseStream;
    }

    public async Task<(string Link, DateTimeOffset ExpiresAt)> GetPresignedUrlAsync(string key)
    {
        var expiredAt = DateTime.UtcNow.AddMinutes(_options.CurrentValue.PresignedUrlDurationMinutes);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = BucketName,
            Key = key,
            Expires = expiredAt
        };

        return (await Task.Run(() => _s3Client.GetPreSignedURL(request)), expiredAt);
    }
}