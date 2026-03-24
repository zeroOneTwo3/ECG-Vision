using EcgVision.Core.Domain.Enums;
using EcgVision.Core.Interfaces;
using EcgVision.Core.Interfaces.Services;
using EcgVision.Infrastructure.Configuration;

using Microsoft.Extensions.Options;

namespace EcgVision.Infrastructure.ExternalServices;

public class S3StorageService : IStorageService
{
    private readonly IOptionsMonitor<StorageOptions> _options;
    private readonly IS3Client _s3Service;

    public StorageLocation Location => StorageLocation.S3;

    public S3StorageService(IS3Client s3Service, IOptionsMonitor<StorageOptions> options)
    {
        _options = options;
        _s3Service = s3Service;
    }

    public Task<Stream> GetAsync(string path)
    {
        return _s3Service.DownloadAsync(path);
    }

    public string GetFullFilePath(string path)
    {
        return $"{GetFolder()}/{path.TrimStart('/')}";
    }

    public string GetFolder()
    {
        return $"{_options.CurrentValue.S3.ServiceUrl.TrimEnd('/')}/{_options.CurrentValue.S3.BucketName}";
    }

    public async Task<(string? Url, DateTimeOffset? ExpiresAt)> GetUrlAsync(string path)
    {
        var result = await _s3Service.GetPresignedUrlAsync(path);
        return (result.Link, result.ExpiresAt);
    }

    public async Task SaveAsync(string path, Stream stream, string contentType)
    {
        await _s3Service.UploadAsync(path, stream, contentType);
    }

    public async Task DeleteAsync(string path)
    {
        // TODO: implement
        //await _s3Service.DeleteAsync(path);
    }
}
