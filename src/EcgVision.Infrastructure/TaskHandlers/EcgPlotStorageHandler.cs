using EcgVision.Core.Domain.Enums;
using EcgVision.Core.Dtos.Tasks;
using EcgVision.Core.Interfaces.Repositories;
using EcgVision.Core.Interfaces.Services;

using Microsoft.Extensions.Logging;

namespace EcgVision.Infrastructure.TaskHandlers;

public class EcgPlotStorageHandler(
    IEcgJobRepository repo,
    IStorageFactory storageFactory,
    ILogger<EcgPlotStorageHandler> logger) : IEcgTaskHandler<SavePlotTask>
{
    public async Task HandleAsync(SavePlotTask task, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(task.Job.Signal?.RawSignalPath))
            throw new ArgumentException("Job signal path is missing.");

        var rawStorage = storageFactory.GetService(DataContext.Raw);
        var plotStorage = storageFactory.GetService(DataContext.Image);

        var fullLocalPath = rawStorage.GetFullFilePath(task.Job.Signal.RawSignalPath);
        var localPngPath = Path.ChangeExtension(fullLocalPath, $"{task.Job.JobType}.png");

        if (!File.Exists(localPngPath))
            throw new FileNotFoundException("Local ECG plot not found for upload.", localPngPath);

        string imageKey = $"plots/{task.Job.Signal.PatientId}/{task.Job.Signal.Id}.{task.Job.JobType}.png";

        await using (var fileStream = new FileStream(
            localPngPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            4096,
            useAsync: true))
        {
            await plotStorage.SaveAsync(imageKey, fileStream, "image/png");
        }

        task.Job.ImagePath = imageKey;
        await repo.UpdateJobStateAsync(task.Job, EcgJobStatus.ImageUploaded, 100);

        logger.LogInformation("Successfully moved plot to external storage for Job: {JobId}", task.Job.Id);
    }
}