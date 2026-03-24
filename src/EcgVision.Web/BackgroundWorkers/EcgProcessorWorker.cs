using EcgVision.Core.Domain.Enums;
using EcgVision.Core.Dtos.Tasks;
using EcgVision.Core.Interfaces;
using EcgVision.Core.Interfaces.Repositories;
using EcgVision.Core.Interfaces.Services;

using Serilog.Context;

namespace EcgVision.Web.BackgroundWorkers;

public class EcgProcessorWorker(
    IBackgroundTaskQueue queue,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<EcgProcessorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("ECG Processor Worker started. Waiting for jobs...");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var jobId = await queue.DequeueAsync(ct);

                // Use LogContext so every log inside the pipeline has the JobId attached
                using (LogContext.PushProperty("JobId", jobId))
                {
                    logger.LogInformation("Job dequeued. Starting pipeline.");
                    await ExecuteJobPipelineAsync(jobId, ct);
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("ECG Processor Worker is shutting down.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Critical error in Dequeue loop.");
            }
        }
    }

    private async Task ExecuteJobPipelineAsync(Guid jobId, CancellationToken ct)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var jobsRepository = scope.ServiceProvider.GetRequiredService<IEcgJobRepository>();

        var job = await jobsRepository.GetByIdWithSignalAsync(jobId);
        if (job?.Signal == null)
        {
            logger.LogError("Job {JobId} not found or missing signal data. Abandoning.", jobId);
            return;
        }

        try
        {
            // --- STAGE 1: PARSING ---
            logger.LogDebug("Stage 1: Extracting signal markers.");
            var extractHandler = scope.ServiceProvider.GetRequiredService<IEcgTaskHandler<ExtractSignalTask>>();
            await extractHandler.HandleAsync(new ExtractSignalTask(job, 4), ct);

            // --- STAGE 2: DATA PERSISTENCE (JSON) ---
            logger.LogDebug("Stage 2: Uploading JSON data.");
            var jsonHandler = scope.ServiceProvider.GetRequiredService<IEcgTaskHandler<SaveJsonTask>>();
            await jsonHandler.HandleAsync(new SaveJsonTask(job), ct);

            // --- STAGE 3: VISUALIZATION ---
            logger.LogDebug("Stage 3: Generating ECG Plot Image.");
            var plotHandler = scope.ServiceProvider.GetRequiredService<IEcgTaskHandler<GeneratePlotTask>>();
            await plotHandler.HandleAsync(new GeneratePlotTask(job), ct);

            // --- STAGE 4: IMAGE STORAGE ---
            logger.LogDebug("Stage 4: Uploading plot to S3.");
            var imageHandler = scope.ServiceProvider.GetRequiredService<IEcgTaskHandler<SavePlotTask>>();
            await imageHandler.HandleAsync(new SavePlotTask(job), ct);

            // --- STAGE 5: COMPLETION ---
            job.CompletedAt = DateTimeOffset.UtcNow;
            await jobsRepository.UpdateJobStateAsync(job, EcgJobStatus.Completed, 100);
            logger.LogInformation("Job {JobId} completed successfully.", jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job {JobId} failed during pipeline execution.", jobId);
            job.CompletedAt = DateTimeOffset.UtcNow;
            await jobsRepository.UpdateJobStateAsync(job, EcgJobStatus.Failed, 0, ex.Message);
        }
        finally
        {
            // Cleanup Landing Zone
        }
    }
}