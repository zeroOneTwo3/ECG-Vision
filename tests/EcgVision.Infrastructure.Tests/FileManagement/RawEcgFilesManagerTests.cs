using System.Text;

using EcgVision.Core.Domain.Entities;
using EcgVision.Core.Domain.Enums;
using EcgVision.Core.Dtos;
using EcgVision.Core.Interfaces;
using EcgVision.Core.Interfaces.Repositories;
using EcgVision.Core.Interfaces.Services;
using EcgVision.Infrastructure.FileManagement;

using Microsoft.Extensions.Logging;

using Moq;

namespace EcgVision.Infrastructure.Tests.FileManagement;

public class RawEcgFilesManagerTests
{
    private readonly Mock<IStorageFactory> _mockStorageFactory;
    private readonly Mock<IStorageService> _mockStorageService;
    private readonly Mock<IEcgSignalRepository> _mockRepo;
    private readonly Mock<IBackgroundTaskQueue> _mockQueue;
    private readonly Mock<ILogger<RawEcgFilesManager>> _mockLogger;
    private readonly RawEcgFilesManager _manager;

    public RawEcgFilesManagerTests()
    {
        _mockStorageFactory = new Mock<IStorageFactory>();
        _mockStorageService = new Mock<IStorageService>();
        _mockRepo = new Mock<IEcgSignalRepository>();
        _mockQueue = new Mock<IBackgroundTaskQueue>();
        _mockLogger = new Mock<ILogger<RawEcgFilesManager>>();

        // Setup the factory to return our mocked storage service for 'Raw' context
        _mockStorageFactory.Setup(f => f.GetService(DataContext.Raw))
            .Returns(_mockStorageService.Object);

        _manager = new RawEcgFilesManager(
            _mockStorageFactory.Object,
            _mockRepo.Object,
            _mockQueue.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void CreateGuidFromRecordName_ShouldBeDeterministic()
    {
        // Arrange
        var recordName = "A1001";

        // Act
        var guid1 = _manager.CreateGuidFromRecordName(recordName);
        var guid2 = _manager.CreateGuidFromRecordName(recordName);

        // Assert
        Assert.Equal(guid1, guid2);
        Assert.NotEqual(Guid.Empty, guid1);
    }

    [Fact]
    public async Task GetRecordNameAsync_ShouldParseFirstValidLineOfHeaFile()
    {
        // Arrange
        var heaContent = "# Comment line\n\nRECORD_123 2 250 3600\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(heaContent));
        var dto = new EcgFilesDto { HeaFileStream = stream };

        // Act
        var result = await _manager.GetRecordNameAsync(dto);

        // Assert
        Assert.Equal("RECORD_123", result);
    }

    [Fact]
    public async Task QueueEcgJobAsync_ShouldNotQueue_WhenDatabaseSaveFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var heaContent = "REC001 2 500";
        var request = CreateMockUploadRequest("patient1", "test.hea", "test.dat", heaContent);

        // Simulate a DB failure
        _mockRepo.Setup(r => r.UpsertWithJobAsync(It.IsAny<EcgSignal>(), It.IsAny<EcgJob>()))
            .ReturnsAsync((null, "Database Connection Error"));

        // Act
        var (jobId, error) = await _manager.QueueEcgJobAsync(request, userId);

        // Assert
        Assert.Null(jobId);
        Assert.Equal("Database Connection Error", error);

        // Ensure nothing was added to the queue!
        _mockQueue.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task QueueEcgJobAsync_ShouldSucceed_WhenFlowIsPerfect()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var generatedJobId = Guid.NewGuid();
        var heaContent = "REC001 2 500";
        var request = CreateMockUploadRequest("p1", "s.hea", "s.dat", heaContent);

        _mockRepo.Setup(r => r.UpsertWithJobAsync(It.IsAny<EcgSignal>(), It.IsAny<EcgJob>()))
            .ReturnsAsync((generatedJobId, null));

        // Act
        var (jobId, error) = await _manager.QueueEcgJobAsync(request, userId);

        // Assert
        Assert.Equal(generatedJobId, jobId);
        Assert.Null(error);

        // Verify S3/Local storage was called twice (HEA and DAT)
        _mockStorageService.Verify(s => s.SaveAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>()), Times.Exactly(2));

        // Verify it was queued
        _mockQueue.Verify(q => q.QueueBackgroundWorkItemAsync(generatedJobId), Times.Once);
    }

    private EcgUploadDto CreateMockUploadRequest(string patientId, string heaName, string datName, string heaContent)
    {
        // Simple mock of the request object
        return new EcgUploadDto
        {
            PatientId = Guid.Empty,
            JobType = JobType.Extract4Leads,
            Files = new EcgFilesDto()
            {
                HeaFileName = heaName,
                DatFileName = datName,
                HeaFileStream = new MockFormFile(heaName, heaContent).OpenReadStream(),
                DatFileStream = new MockFormFile(datName, "binarydata").OpenReadStream()
            }
        };
    }
}

// Minimal helper to mock IFormFile if your DTO uses it
public class MockFormFile(string fileName, string content) : Microsoft.AspNetCore.Http.IFormFile
{
    public string ContentType => "text/plain";
    public string ContentDisposition => "";
    public Microsoft.AspNetCore.Http.IHeaderDictionary Headers => new Microsoft.AspNetCore.Http.HeaderDictionary();
    public long Length => content.Length;
    public string Name => "file";
    public string FileName => fileName;
    public Stream OpenReadStream() => new MemoryStream(Encoding.UTF8.GetBytes(content));
    public void CopyTo(Stream target) => OpenReadStream().CopyTo(target);
    public Task CopyToAsync(Stream target, CancellationToken ct = default) => OpenReadStream().CopyToAsync(target, ct);
}