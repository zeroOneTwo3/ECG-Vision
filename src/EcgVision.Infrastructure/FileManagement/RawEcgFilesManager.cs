using System.Security.Cryptography;
using System.Text;

using EcgVision.Core.Domain.Entities;
using EcgVision.Core.Domain.Enums;
using EcgVision.Core.Dtos;
using EcgVision.Core.Interfaces;
using EcgVision.Core.Interfaces.Repositories;
using EcgVision.Core.Interfaces.Services;

using Microsoft.Extensions.Logging;

namespace EcgVision.Infrastructure.FileManagement;

public class RawEcgFilesManager : IRawEcgFilesManager
{
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IEcgSignalRepository _ecgSignalRepository;
    private readonly IStorageService _storageService;
    private readonly ILogger<RawEcgFilesManager> _logger;

    public RawEcgFilesManager(
        IStorageFactory storageFactory,
        IEcgSignalRepository ecgSignalRepository,
        IBackgroundTaskQueue backgroundTaskQueue,
        ILogger<RawEcgFilesManager> logger)
    {
        _storageService = storageFactory.GetService(DataContext.Raw);
        _ecgSignalRepository = ecgSignalRepository;
        _backgroundTaskQueue = backgroundTaskQueue;
        _logger = logger;
    }

    public Guid CreateGuidFromRecordName(string recordName)
    {
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(recordName));
        return new Guid(hash);
    }

    public async Task<(Guid? JobId, string? ErrorMessage)> QueueEcgJobAsync(EcgUploadDto ecgDto, Guid userId)
    {
        var recordName = await GetRecordNameAsync(ecgDto.Files);

        if (string.IsNullOrEmpty(recordName))
            return (null, "Invalid .hea file: Record name not found.");

        // Create a deterministic GUID from the record name
        var signalId = CreateGuidFromRecordName(recordName);
        var rawPath = await SaveAsync(ecgDto.Files, signalId.ToString());

        var newJob = new EcgJob
        {
            Id = Guid.NewGuid(),
            SignalId = signalId,
            Status = EcgJobStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UserId = ecgDto.UserId.ToString(),
            JobType = ecgDto.JobType
        };

        var signal = new EcgSignal
        {
            Id = signalId,
            PatientId = ecgDto.PatientId.ToString(),
            RawSignalPath = rawPath, // Worker will look here
            CreatedAt = DateTimeOffset.UtcNow
        };

        (Guid? jobId, string? error) = await _ecgSignalRepository.UpsertWithJobAsync(signal, newJob);
        if (jobId == null || !string.IsNullOrWhiteSpace(error))
            return (null, string.IsNullOrWhiteSpace(error) ? "Could not create entities" : error);

        await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(jobId.Value);

        return (jobId, null);
    }

    public async Task<string?> GetRecordNameAsync(EcgFilesDto ecgFilesDto)
    {
        using var reader = new StreamReader(ecgFilesDto.HeaFileStream, leaveOpen: true);

        if (ecgFilesDto.HeaFileStream.CanSeek)
            ecgFilesDto.HeaFileStream.Position = 0;

        string? line;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            line = line.Trim();

            // Skip empty lines or comments starting with #
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;

            if (ecgFilesDto.HeaFileStream.CanSeek)
                ecgFilesDto.HeaFileStream.Position = 0;

            // The first word of the first valid line is the Record Name
            return line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).First();
        }

        return null;
    }

    public async Task<string> SaveAsync(EcgFilesDto ecgFilesDto, string patientId)
    {
        var heaPath = $"{patientId}/{ecgFilesDto.HeaFileName}";
        var datPath = $"{patientId}/{ecgFilesDto.DatFileName}";

        await SaveFileAsync(heaPath, ecgFilesDto.HeaFileStream, "text/plain");
        await SaveFileAsync(datPath, ecgFilesDto.DatFileStream, "application/octet-stream");

        return Path.ChangeExtension(heaPath, null); // return path without .hea extension
    }

    private async Task SaveFileAsync(string path, Stream fileStream, string contentType)
    {
        if (fileStream.CanSeek)
            fileStream.Position = 0;

        await _storageService.SaveAsync(path, fileStream, contentType);
    }
}
