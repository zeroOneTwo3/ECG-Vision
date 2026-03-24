using System.Text;

using EcgVision.Infrastructure.Configuration;
using EcgVision.Infrastructure.FileManagement;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

using Moq;

namespace EcgVision.Infrastructure.Tests.FileManagement;

public class LocalStorageServiceTests : IDisposable
{
    private readonly string _testRoot;
    private readonly Mock<IWebHostEnvironment> _mockEnv;
    private readonly Mock<IOptionsMonitor<LocalStorageOptions>> _mockOptions;
    private readonly LocalStorageService _service;

    public LocalStorageServiceTests()
    {
        // Create a unique temporary directory for this test run
        _testRoot = Path.Combine(Path.GetTempPath(), "EcgVisionTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testRoot);

        _mockEnv = new Mock<IWebHostEnvironment>();
        _mockEnv.Setup(m => m.ContentRootPath).Returns(_testRoot);

        // Setup options to point to our test folders
        var options = new LocalStorageOptions
        {
            TempRoot = "TestTemp",
            LandingZone = "TestLanding"
        };

        _mockOptions = new Mock<IOptionsMonitor<LocalStorageOptions>>();
        _mockOptions.Setup(m => m.CurrentValue).Returns(options);

        _service = new LocalStorageService(_mockEnv.Object, _mockOptions.Object);
    }

    [Fact]
    public async Task SaveAsync_ShouldCreateFileOnDisk()
    {
        // Arrange
        var fileName = "test_signal.dat";
        var content = "dummy ecg data";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        await _service.SaveAsync(fileName, stream, "application/octet-stream");

        // Assert
        var expectedPath = _service.GetFullFilePath(fileName);
        Assert.True(File.Exists(expectedPath));
        var savedContent = await File.ReadAllTextAsync(expectedPath);
        Assert.Equal(content, savedContent);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnValidStream_WhenFileExists()
    {
        // Arrange
        var fileName = "readable.dat";
        var expectedContent = "120bpm_sample";
        var fullPath = _service.GetFullFilePath(fileName);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, expectedContent);

        // Act
        using var resultStream = await _service.GetAsync(fileName);
        using var reader = new StreamReader(resultStream);
        var actualContent = await reader.ReadToEndAsync();

        // Assert
        Assert.Equal(expectedContent, actualContent);
    }

    [Fact]
    public async Task GetAsync_ShouldThrowFileNotFound_WhenFileIsMissing()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _service.GetAsync("non_existent.dat"));
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveFileFromDisk()
    {
        // Arrange
        var fileName = "to_delete.dat";
        var fullPath = _service.GetFullFilePath(fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, "temp");

        // Act
        await _service.DeleteAsync(fileName);

        // Assert
        Assert.False(File.Exists(fullPath));
    }

    // Cleanup after every test to keep the disk clean
    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, true);
        }
    }
}