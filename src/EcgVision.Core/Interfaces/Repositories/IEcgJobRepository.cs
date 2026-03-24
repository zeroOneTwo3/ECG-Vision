using EcgVision.Core.Domain.Entities;
using EcgVision.Core.Domain.Enums;

namespace EcgVision.Core.Interfaces.Repositories;

public interface IEcgJobRepository
{
    Task<EcgJob?> GetByIdWithSignalAsync(Guid id);
    Task<EcgJob?> GetBySignalIdAsync(Guid signalId);
    Task CreateAsync(EcgJob job);
    Task UpdateJobStateAsync(EcgJob job, EcgJobStatus status, int progressPercentage, string? error = null);
    Task<IEnumerable<EcgJob>> GetPendingJobsAsync();
}
