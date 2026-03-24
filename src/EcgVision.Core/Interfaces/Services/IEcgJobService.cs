using EcgVision.Core.Domain.Entities;
using EcgVision.Core.Dtos;

namespace EcgVision.Core.Interfaces.Services;

public interface IEcgJobService
{
    Task<EcgJob?> GetByIdWithSignalAsync(Guid jobId);

    Task<EcgPlotUrlResponse> GetPlotPresignedUrlAsync(EcgJob ecgJob);
}
