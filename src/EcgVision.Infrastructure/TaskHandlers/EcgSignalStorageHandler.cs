using EcgVision.Core.Domain.Enums;
using EcgVision.Core.Dtos.Tasks;
using EcgVision.Core.Interfaces.Repositories;
using EcgVision.Core.Interfaces.Services;

using Microsoft.Extensions.Logging;

namespace EcgVision.Infrastructure.TaskHandlers;

public class EcgSignalStorageHandler(
    IEcgJobRepository repo,
    IStorageFactory storageFactory,
    ILogger<EcgSignalStorageHandler> logger) : IEcgTaskHandler<SaveJsonTask>
{
    public async Task HandleAsync(SaveJsonTask task, CancellationToken ct)
    {
        var jobId = task.Job.Id;
        var signal = task.Job.Signal;

        if (string.IsNullOrWhiteSpace(signal?.RawSignalPath))
        {
            logger.LogError("Storage failed: Missing Signal or RawSignalPath for Job: {JobId}", jobId);
            throw new ArgumentException("Job signal path is missing.");
        }

        try
        {
            var rawStorage = storageFactory.GetService(DataContext.Raw);
            var jsonStorage = storageFactory.GetService(DataContext.Json);

            var fullRawPath = rawStorage.GetFullFilePath(signal.RawSignalPath);
            var localJsonPath = Path.ChangeExtension(fullRawPath, $"{task.Job.JobType}.json");

            if (!File.Exists(localJsonPath))
            {
                logger.LogError("Local JSON file not found at {Path} for Job: {JobId}", localJsonPath, jobId);
                throw new FileNotFoundException("Local extracted JSON leads not found.", localJsonPath);
            }

            var jsonKey = $"signals/{signal.PatientId}/{signal.Id}.{task.Job.JobType}.json";

            logger.LogInformation("Uploading JSON leads to cloud storage for Job: {JobId}. Key: {Key}", jobId, jsonKey);

            await using (var fileStream = new FileStream(
                localJsonPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true))
            {
                await jsonStorage.SaveAsync(jsonKey, fileStream, "application/json");
            }

            task.Job.JsonPath = jsonKey;
            await repo.UpdateJobStateAsync(task.Job, EcgJobStatus.JsonUploaded, 30);

            logger.LogInformation("Successfully migrated JSON leads to cloud for Job: {JobId}", jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while uploading JSON leads for Job: {JobId}", jobId);
            throw;
        }
    }
}