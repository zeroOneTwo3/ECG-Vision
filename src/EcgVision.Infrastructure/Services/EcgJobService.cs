using EcgVision.Core.Domain.Entities;
using EcgVision.Core.Domain.Enums;
using EcgVision.Core.Dtos;
using EcgVision.Core.Interfaces.Repositories;
using EcgVision.Core.Interfaces.Services;

using Microsoft.Extensions.Logging;

namespace EcgVision.Infrastructure.Services;

public class EcgJobService : IEcgJobService
{
    private readonly IEcgJobRepository _ecgJobRepository;
    private readonly IStorageService _storageService;
    private readonly ILogger<EcgJobService> _logger;

    public EcgJobService(
        IEcgJobRepository ecgJobRepository,
        IStorageFactory storageFactory,
        ILogger<EcgJobService> logger)
    {
        _ecgJobRepository = ecgJobRepository;
        _storageService = storageFactory.GetService(DataContext.Image);
        _logger = logger;
    }
    public async Task<EcgJob?> GetByIdWithSignalAsync(Guid jobId)
    {
        return await _ecgJobRepository.GetByIdWithSignalAsync(jobId);
    }

    public async Task<EcgPlotUrlResponse> GetPlotPresignedUrlAsync(EcgJob ecgJob)
    {
        if (ecgJob.Status != EcgJobStatus.Completed || string.IsNullOrEmpty(ecgJob.ImagePath))
            return EcgPlotUrlResponse.Failure("Result is not ready for download.");

        // Generate a time-limited link
        var expiration = TimeSpan.FromHours(1);
        var linkData = await _storageService.GetUrlAsync(ecgJob.ImagePath);

        return EcgPlotUrlResponse.Success(linkData.Url!, linkData.ExpiresAt!.Value);
    }
}
