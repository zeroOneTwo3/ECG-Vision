using EcgVision.Core.Domain.Entities;
using EcgVision.Core.Interfaces.Repositories;
using EcgVision.Core.Interfaces.Services;

namespace EcgVision.Infrastructure.Services;

public class PatientService(IUserRepository userRepository, IEcgSignalRepository signalRepository) : IPatientService
{
    public async Task<bool> ExistsAsync(Guid patientId)
    {
        return await userRepository.GetByIdAsync(patientId) != null;
    }

    public async Task<IEnumerable<EcgSignal>> GetSignalsWithJobsAsync(Guid patientId)
    {
        return await signalRepository.GetByPatientIdAsync(patientId.ToString());
    }
}
