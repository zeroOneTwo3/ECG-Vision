using System.Diagnostics;
using System.Text.Json;

using EcgVision.Core.Domain.Enums;
using EcgVision.Core.Dtos;
using EcgVision.Core.Dtos.Tasks;
using EcgVision.Core.Interfaces;
using EcgVision.Core.Interfaces.Repositories;
using EcgVision.Core.Interfaces.Services;

using Microsoft.Extensions.Logging;

namespace EcgVision.Infrastructure.TaskHandlers;

public class EcgPlotGenerationHandler(
    IEcgJobRepository repo,
    IStorageFactory storageFactory,
    IEcgImageGenerator ecgImageGenerator,
    ILogger<EcgPlotGenerationHandler> logger) : IEcgTaskHandler<GeneratePlotTask>
{
    public async Task HandleAsync(GeneratePlotTask task, CancellationToken ct)
    {
        var jobId = task.Job.Id;
        var sw = Stopwatch.StartNew();

        try
        {
            var rawPath = task.Job.Signal?.RawSignalPath;
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                logger.LogError("Plot generation failed: Missing Signal path for Job: {JobId}", jobId);
                throw new ArgumentException("Job signal path is missing.");
            }

            var rawStorage = storageFactory.GetService(DataContext.Raw);
            var fullRawPath = rawStorage.GetFullFilePath(rawPath);
            var jsonPath = Path.ChangeExtension(fullRawPath, $"{task.Job.JobType}.json");
            var pngPath = Path.ChangeExtension(fullRawPath, $"{task.Job.JobType}.png");

            if (!File.Exists(jsonPath))
            {
                logger.LogError("Required JSON data missing at {Path} for Job: {JobId}", jsonPath, jobId);
                throw new FileNotFoundException("Extracted leads JSON not found.", jsonPath);
            }

            logger.LogInformation("Starting plot generation for Job: {JobId}. Mode: {Type}", jobId, task.Job.JobType);

            await repo.UpdateJobStateAsync(task.Job, EcgJobStatus.GeneratingImage, 60);

            EcgLeadsDto leads;
            logger.LogDebug("Deserializing leads for Job: {JobId}", jobId);

            await using (var stream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                leads = await JsonSerializer.DeserializeAsync<EcgLeadsDto>(stream, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }, ct) ?? throw new InvalidOperationException("Failed to deserialize ECG leads.");
            }

            logger.LogDebug("Invoking ImageGenerator for {JobId}. Lead Count: {Count}", jobId, leads.LeadCount);

            byte[] imageBytes = ecgImageGenerator.GenerateLeadsPlot(leads, task.Job.JobType);

            await File.WriteAllBytesAsync(pngPath, imageBytes, ct);

            sw.Stop();
            logger.LogInformation("Plot generated and saved locally in {ElapsedMs}ms for Job: {JobId}", sw.ElapsedMilliseconds, jobId);

            await repo.UpdateJobStateAsync(task.Job, EcgJobStatus.ImageUploaded, 90);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during plot generation for Job: {JobId}", jobId);
            throw;
        }
    }
}