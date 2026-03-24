using EcgVision.Core.Domain.Enums;

namespace EcgVision.Core.Interfaces.Services;

public interface IStorageService
{
    StorageLocation Location { get; }
    string GetFullFilePath(string path);
    string GetFolder();
    Task SaveAsync(string path, Stream stream, string contentType);
    Task<Stream> GetAsync(string name);
    Task<(string? Url, DateTimeOffset? ExpiresAt)> GetUrlAsync(string path);
    Task DeleteAsync(string path);
}
