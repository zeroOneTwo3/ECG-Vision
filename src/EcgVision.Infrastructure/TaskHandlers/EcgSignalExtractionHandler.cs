using System.Text.Json;
using System.Text.Json.Serialization;

using EcgVision.Core.Domain.Enums;
using EcgVision.Core.Dtos.Tasks;
using EcgVision.Core.Interfaces;
using EcgVision.Core.Interfaces.Repositories;
using EcgVision.Core.Interfaces.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EcgVision.Infrastructure.TaskHandlers;

public class EcgSignalExtractionHandler(
    IServiceScopeFactory scopeFactory,
    IEcgSignalParser parser,
    ILogger<EcgSignalExtractionHandler> logger) : IEcgTaskHandler<ExtractSignalTask>
{
    public async Task HandleAsync(ExtractSignalTask task, CancellationToken ct)
    {
        var jobId = task.Job.Id;
        var signalPath = task.Job.Signal?.RawSignalPath;

        if (string.IsNullOrWhiteSpace(signalPath))
        {
            logger.LogError("Extraction failed: RawSignalPath is missing for Job: {JobId}", jobId);
            throw new FileNotFoundException("Signal path is null or empty.");
        }

        logger.LogInformation("Starting signal extraction for Job: {JobId}. Signal Source: {Path}", jobId, signalPath);

        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IEcgJobRepository>();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageFactory>().GetService(DataContext.Raw);

        var localPath = storageService.GetFullFilePath(signalPath);
        var leadsCount = task.Job.JobType == JobType.Extract4Leads ? 4 : 12;

        try
        {
            logger.LogDebug("Parsing {LeadsCount} leads from {LocalPath} for Job: {JobId}", leadsCount, localPath, jobId);

            var leads = await parser.ExtractLeadsAsync(localPath, leadsCount);

            await repo.UpdateJobStateAsync(task.Job, EcgJobStatus.Parsing, 10);

            var json = JsonSerializer.Serialize(leads, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            var outputPath = $"{localPath}.{task.Job.JobType}.json";
            await File.WriteAllTextAsync(outputPath, json, ct);

            logger.LogInformation("Successfully extracted and serialized {LeadsCount} leads to {OutputPath} for Job: {JobId}",
                leadsCount, outputPath, jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled error occurred during lead extraction for Job: {JobId}. Lead Count: {Count}",
                jobId, leadsCount);
            throw;
        }
    }
}