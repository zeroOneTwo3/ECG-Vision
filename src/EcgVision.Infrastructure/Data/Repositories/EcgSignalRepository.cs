using EcgVision.Core.Domain.Entities;
using EcgVision.Core.Interfaces.Repositories;

using Microsoft.EntityFrameworkCore;

namespace EcgVision.Infrastructure.Data.Repositories;

public class EcgSignalRepository(ApplicationDbContext context) : IEcgSignalRepository
{
    public async Task<EcgSignal?> GetByIdAsync(Guid id) =>
        await context.EcgSignals
            .Include(s => s.Jobs)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<IEnumerable<EcgSignal>> GetByPatientIdAsync(string patientId) =>
        await context.EcgSignals
            .Include(s => s.Jobs)
            .Where(s => s.PatientId == patientId)
            .AsNoTracking()
            .ToListAsync();

    public async Task CreateAsync(EcgSignal signal)
    {
        await context.EcgSignals.AddAsync(signal);
        await context.SaveChangesAsync();
    }

    public async Task<(Guid? JobId, string? ErrorMessage)> UpsertWithJobAsync(EcgSignal ecgSignal, EcgJob ecgJob)
    {
        // Using ExecutionStrategy for resilient cloud connections
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<(Guid? JobId, string? ErrorMessage)>(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var existingSignal = await context.EcgSignals
                    .FirstOrDefaultAsync(e => e.Id == ecgSignal.Id);

                if (existingSignal == null)
                {
                    ecgSignal.Jobs = [ecgJob];
                    ecgSignal.UpdatedAt = DateTimeOffset.UtcNow;
                    await context.EcgSignals.AddAsync(ecgSignal);
                }
                else
                {
                    if (existingSignal.PatientId != ecgSignal.PatientId)
                        return (null, "Conflict: Signal ID belongs to a different patient.");

                    // Note: Path manipulation should ideally happen before calling the repo
                    existingSignal.RawSignalPath = ecgSignal.RawSignalPath;
                    existingSignal.UpdatedAt = DateTimeOffset.UtcNow;

                    ecgJob.SignalId = existingSignal.Id;
                    await context.EcgJobs.AddAsync(ecgJob);
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                return (ecgJob.Id, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return (null, $"Database failure: {ex.Message}");
            }
        });
    }

    public async Task UpdateAsync(EcgSignal signal)
    {
        context.EcgSignals.Update(signal);
        await context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Guid id) =>
        await context.EcgSignals.AnyAsync(s => s.Id == id);
}