using EcgVision.Core.Domain.Entities;
using EcgVision.Core.Domain.Enums;
using EcgVision.Infrastructure.Data;
using EcgVision.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EcgVision.Infrastructure.Tests.Repositories;

public class EcgSignalRepositoryTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task UpsertWithJobAsync_ShouldCreateNewSignalAndJob_WhenSignalDoesNotExist()
    {
        // Arrange
        using var context = GetDbContext();
        var repo = new EcgSignalRepository(context);
        var signalId = Guid.NewGuid();
        var signal = new EcgSignal { Id = signalId, PatientId = "P-101", RawSignalPath = "raw.dat" };
        var job = new EcgJob { Id = Guid.NewGuid(), Status = EcgJobStatus.Pending };

        // Act
        var (jobId, error) = await repo.UpsertWithJobAsync(signal, job);

        // Assert
        Assert.Null(error);
        Assert.Equal(job.Id, jobId);

        var dbSignal = await context.EcgSignals.Include(s => s.Jobs).FirstOrDefaultAsync(s => s.Id == signalId);
        Assert.NotNull(dbSignal);
        Assert.Single(dbSignal.Jobs);
        Assert.Equal("P-101", dbSignal.PatientId);
    }

    [Fact]
    public async Task UpsertWithJobAsync_ShouldAddJobToExistingSignal_WhenSignalExists()
    {
        // Arrange
        using var context = GetDbContext();
        var repo = new EcgSignalRepository(context);
        var signalId = Guid.NewGuid();

        // Pre-seed an existing signal
        var existing = new EcgSignal
        {
            Id = signalId,
            PatientId = "P-101",
            RawSignalPath = "old.dat",
            Jobs = [new EcgJob()]
        };
        context.EcgSignals.Add(existing);
        await context.SaveChangesAsync();

        var newJob = new EcgJob { Id = Guid.NewGuid(), Status = EcgJobStatus.Pending };
        var signalUpdateInfo = new EcgSignal { Id = signalId, PatientId = "P-101", RawSignalPath = "new.dat" };

        // Act
        var (jobId, error) = await repo.UpsertWithJobAsync(signalUpdateInfo, newJob);

        // Assert
        Assert.Null(error);
        var dbSignal = await context.EcgSignals.Include(s => s.Jobs).FirstOrDefaultAsync(s => s.Id == signalId);
        Assert.Equal(2, await context.EcgJobs.CountAsync()); // One initial (if any) + one new
        Assert.Equal("new.dat", dbSignal?.RawSignalPath); // Verifies path update
    }

    [Fact]
    public async Task UpsertWithJobAsync_ShouldReturnError_WhenPatientIdDoesNotMatch()
    {
        // Arrange
        using var context = GetDbContext();
        var repo = new EcgSignalRepository(context);
        var signalId = Guid.NewGuid();

        // Signal exists for Patient A
        context.EcgSignals.Add(new EcgSignal { Id = signalId, PatientId = "Patient-A" });
        await context.SaveChangesAsync();

        // Attempt to upsert same Signal ID for Patient B
        var conflictingSignal = new EcgSignal { Id = signalId, PatientId = "Patient-B" };
        var job = new EcgJob { Id = Guid.NewGuid() };

        // Act
        var (jobId, error) = await repo.UpsertWithJobAsync(conflictingSignal, job);

        // Assert
        Assert.Null(jobId);
        Assert.Contains("belongs to a different patient", error);

        // Verify no new job was actually added (Transaction Integrity)
        var jobCount = await context.EcgJobs.CountAsync();
        Assert.Equal(0, jobCount);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenSignalNotFound()
    {
        // Arrange
        using var context = GetDbContext();
        var repo = new EcgSignalRepository(context);

        // Act
        var result = await repo.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByPatientIdAsync_ShouldReturnSignalsWithIncludedJobs()
    {
        // Arrange
        using var context = GetDbContext();
        var repository = new EcgSignalRepository(context);

        var patientId = "patient-123";
        var signalId = Guid.NewGuid();

        var signal = new EcgSignal
        {
            Id = signalId,
            PatientId = patientId,
            RawSignalPath = "path/to/raw",
            Jobs = new List<EcgJob>
            {
                new() { Id = Guid.NewGuid(), Status = EcgJobStatus.Completed, JobType = JobType.BaselineScreening },
                new() { Id = Guid.NewGuid(), Status = EcgJobStatus.Failed, JobType = JobType.Extract4Leads }
            }
        };

        context.EcgSignals.Add(signal);
        await context.SaveChangesAsync();

        // Act
        var results = await repository.GetByPatientIdAsync(patientId);
        var resultList = results.ToList();

        // Assert
        Assert.Single(resultList);
        var retrievedSignal = resultList.First();

        Assert.Equal(patientId, retrievedSignal.PatientId);

        // CRITICAL: This proves the .Include(s => s.Jobs) worked!
        Assert.NotNull(retrievedSignal.Jobs);
        Assert.Equal(2, retrievedSignal.Jobs.Count);
        Assert.Contains(retrievedSignal.Jobs, j => j.Status == EcgJobStatus.Completed);
    }

    [Fact]
    public async Task GetByPatientIdAsync_ShouldReturnEmpty_WhenPatientHasNoSignals()
    {
        // Arrange
        using var context = GetDbContext();
        var repository = new EcgSignalRepository(context);

        // Act
        var results = await repository.GetByPatientIdAsync("non-existent-patient");

        // Assert
        Assert.Empty(results);
    }
}