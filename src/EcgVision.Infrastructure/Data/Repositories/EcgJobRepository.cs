using EcgVision.Core.Domain.Entities;
using EcgVision.Core.Domain.Enums;
using EcgVision.Core.Interfaces.Repositories;

using Microsoft.EntityFrameworkCore;

namespace EcgVision.Infrastructure.Data.Repositories;

public class EcgJobRepository(ApplicationDbContext context) : IEcgJobRepository
{
    public async Task<EcgJob?> GetByIdWithSignalAsync(Guid id) =>
        await context.EcgJobs
            .Include(j => j.Signal)
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id);

    public async Task CreateAsync(EcgJob job)
    {
        await context.EcgJobs.AddAsync(job);
        await context.SaveChangesAsync();
    }

    public async Task UpdateJobStateAsync(EcgJob job, EcgJobStatus status, int progress, string? errorMessage = null)
    {
        var entry = context.Entry(job);
        if (entry.State == EntityState.Detached)
        {
            context.EcgJobs.Attach(job);
        }

        job.Status = status;
        job.ProgressPercentage = progress;

        if (!string.IsNullOrWhiteSpace(errorMessage))
            job.ErrorMessage = errorMessage;

        await context.SaveChangesAsync();
    }

    public async Task<EcgJob?> GetBySignalIdAsync(Guid signalId) =>
        await context.EcgJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == signalId);

    public async Task<IEnumerable<EcgJob>> GetPendingJobsAsync() =>
        await context.EcgJobs
            .Where(j => j.Status == EcgJobStatus.Pending)
            .AsNoTracking()
            .ToListAsync();
}