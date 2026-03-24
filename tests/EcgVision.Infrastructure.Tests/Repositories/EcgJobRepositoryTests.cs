using EcgVision.Core.Domain.Entities;
using EcgVision.Core.Domain.Enums;
using EcgVision.Infrastructure.Data;
using EcgVision.Infrastructure.Data.Repositories;

using Microsoft.EntityFrameworkCore;

namespace EcgVision.Infrastructure.Tests.Repositories;

public class EcgJobRepositoryTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistJobToDatabase()
    {
        // Arrange
        using var context = GetDbContext();
        var repo = new EcgJobRepository(context);
        var job = new EcgJob { Id = Guid.NewGuid(), Status = EcgJobStatus.Pending };

        // Act
        await repo.CreateAsync(job);

        // Assert
        var savedJob = await context.EcgJobs.FindAsync(job.Id);
        Assert.NotNull(savedJob);
        Assert.Equal(EcgJobStatus.Pending, savedJob.Status);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldIncludeSignalData()
    {
        // Arrange
        using var context = GetDbContext();
        var repo = new EcgJobRepository(context);
        var jobId = Guid.NewGuid();
        var job = new EcgJob
        {
            Id = jobId,
            Signal = new EcgSignal { Id = Guid.NewGuid(), RawSignalPath = "path/to/raw" }
        };

        context.EcgJobs.Add(job);
        await context.SaveChangesAsync();

        // Act
        var result = await repo.GetByIdWithSignalAsync(jobId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Signal); // Verifies the .Include() logic
        Assert.Equal("path/to/raw", result.Signal.RawSignalPath);
    }

    [Fact]
    public async Task UpdateJobStateAsync_ShouldUpdateMultipleFields()
    {
        // Arrange
        using var context = GetDbContext();
        var repo = new EcgJobRepository(context);
        var job = new EcgJob { Id = Guid.NewGuid(), Status = EcgJobStatus.Pending, ProgressPercentage = 0 };
        context.EcgJobs.Add(job);
        await context.SaveChangesAsync();
        var newJobState = EcgJobStatus.Parsing;
        var newProgressPercentage = 30;
        var errorMessage = "No errors";

        // Act
        await repo.UpdateJobStateAsync(job, newJobState, newProgressPercentage, errorMessage);

        // Assert
        var updatedJob = await context.EcgJobs.FindAsync(job.Id);
        Assert.Equal(newJobState, updatedJob?.Status);
        Assert.Equal(newProgressPercentage, updatedJob?.ProgressPercentage);
        Assert.Equal(errorMessage, updatedJob?.ErrorMessage);
    }

    [Fact]
    public async Task GetPendingJobsAsync_ShouldOnlyReturnPendingStatus()
    {
        // Arrange
        using var context = GetDbContext();
        var repo = new EcgJobRepository(context);
        context.EcgJobs.AddRange(
            new EcgJob { Id = Guid.NewGuid(), Status = EcgJobStatus.Pending },
            new EcgJob { Id = Guid.NewGuid(), Status = EcgJobStatus.Completed },
            new EcgJob { Id = Guid.NewGuid(), Status = EcgJobStatus.Pending }
        );
        await context.SaveChangesAsync();

        // Act
        var results = await repo.GetPendingJobsAsync();

        // Assert
        Assert.Equal(2, results.Count());
        Assert.All(results, j => Assert.Equal(EcgJobStatus.Pending, j.Status));
    }
}