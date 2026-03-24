using EcgVision.Core.Domain.Entities;

namespace EcgVision.Core.Interfaces.Services;

public interface IPatientService
{
    Task<bool> ExistsAsync(Guid patientId);
    Task<IEnumerable<EcgSignal>> GetSignalsWithJobsAsync(Guid patientId);
}
