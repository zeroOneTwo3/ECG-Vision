using EcgVision.Core.Domain.Enums;
using EcgVision.Core.Interfaces.Services;
using EcgVision.Infrastructure.Configuration;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace EcgVision.Infrastructure.FileManagement;

public class LocalStorageService(IWebHostEnvironment env, IOptionsMonitor<LocalStorageOptions> options) : IStorageService
{
    public StorageLocation Location => StorageLocation.Local;

    private string GetTargetFolder()
    {
        var folder = Path.Combine(
            env.ContentRootPath,
            options.CurrentValue.TempRoot,
            options.CurrentValue.LandingZone);

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        return folder;
    }

    public Task<Stream> GetAsync(string path)
    {
        string fullPath = GetFullFilePath(path);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("ECG raw data not found", fullPath);

        // Combine Asynchronous (for Task-based I/O) and SequentialScan (for read-ahead optimization)
        var options = FileOptions.Asynchronous | FileOptions.SequentialScan;

        // We do NOT use 'using' here because we want the stream to stay open for the caller
        var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            options: options); // Important for high-throughput async I/O

        return Task.FromResult<Stream>(stream);
    }

    public string GetFullFilePath(string path)
    {
        var safePathName = Path.GetDirectoryName(path) ?? "";
        var safeFileName = Path.GetFileName(path);
        return Path.Combine(GetTargetFolder(), safePathName, safeFileName);
    }

    public string GetFolder() => GetTargetFolder();

    public Task<(string? Url, DateTimeOffset? ExpiresAt)> GetUrlAsync(string path)
    {
        var fullPath = GetFullFilePath(path);
        var fileInfo = new FileInfo(fullPath);

        if (!fileInfo.Exists)
        {
            return Task.FromResult<(string? Url, DateTimeOffset? ExpiresAt)>((null, null));
        }

        // Calculate how much life is left in the file
        var maxLife = TimeSpan.FromHours(options.CurrentValue.StorageDurationHours);
        var age = DateTime.UtcNow - fileInfo.LastWriteTimeUtc;
        var remainingLife = maxLife - age;

        // Safety: If the file is already past its cleanup time, return 0 seconds left
        var secondsRemaining = Math.Max(0, remainingLife.TotalSeconds);
        var expiredAt = DateTimeOffset.UtcNow.AddSeconds(secondsRemaining);

        var url = $"/api/storage/local/{path.Replace("\\", "/")}";
        return Task.FromResult<(string? Url, DateTimeOffset? ExpiresAt)>((url, expiredAt));
    }

    public async Task SaveAsync(string path, Stream stream, string contentType)
    {
        string fullPath = GetFullFilePath(path);
        string? directory = Path.GetDirectoryName(fullPath);

        if (directory != null)
            Directory.CreateDirectory(directory);

        await using var fileStream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            4096,
            useAsync: true);

        await stream.CopyToAsync(fileStream);
    }

    public Task DeleteAsync(string path)
    {
        string fullPath = GetFullFilePath(path);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("ECG raw data not found", fullPath);

        File.Delete(fullPath);

        return Task.CompletedTask;
    }
}
