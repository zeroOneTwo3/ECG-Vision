using EcgVision.Core.Domain.Enums;
using EcgVision.Core.Interfaces.Services;
using EcgVision.Infrastructure.Configuration;

using Microsoft.Extensions.Options;

namespace EcgVision.Web.BackgroundWorkers;

public class TempCleanupService(
    IOptionsMonitor<LocalStorageOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<TempCleanupService> logger) : BackgroundService
{
    private TimeSpan GetLifetime() => TimeSpan.FromHours(options.CurrentValue.StorageDurationHours);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Janitor Service started. Cleanup interval: 1 hour.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var storageFactory = scope.ServiceProvider.GetRequiredService<IStorageFactory>();
                var storageService = storageFactory.GetService(DataContext.Raw);

                CleanOrphanedFolders(storageService);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Scheduled cleanup of temp_uploads failed.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private void CleanOrphanedFolders(IStorageService storageService)
    {
        var folder = storageService.GetFolder();
        if (!Directory.Exists(folder)) return;

        var directories = Directory.GetDirectories(folder);
        foreach (var dir in directories)
        {
            var dirInfo = new DirectoryInfo(dir);

            if (DateTime.UtcNow - dirInfo.LastWriteTimeUtc > GetLifetime())
            {
                try
                {
                    dirInfo.Delete(recursive: true);
                    logger.LogInformation("Janitor: Cleaned up orphaned folder {Path}", dir);
                }
                catch (IOException)
                {
                    logger.LogWarning("Janitor: Could not delete {Path}. File likely in use.", dir);
                }
            }
        }
    }
}