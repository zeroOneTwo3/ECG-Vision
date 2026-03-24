using EcgVision.Core.Domain.Entities;

namespace EcgVision.Core.Interfaces.Repositories;

public interface IEcgSignalRepository
{
    Task<EcgSignal?> GetByIdAsync(Guid id);
    Task<IEnumerable<EcgSignal>> GetByPatientIdAsync(string patientId);
    Task CreateAsync(EcgSignal signal);
    Task<(Guid? JobId, string? ErrorMessage)> UpsertWithJobAsync(EcgSignal ecgSignal, EcgJob ecgJob);
    Task UpdateAsync(EcgSignal signal);
    Task<bool> ExistsAsync(Guid id);
}
