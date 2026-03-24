using EcgVision.Core.Interfaces;
using EcgVision.Infrastructure.Configuration;
using EcgVision.Infrastructure.ExternalServices;

using Microsoft.Extensions.Options;

using Moq;

namespace EcgVision.Infrastructure.Tests.ExternalServices;

public class S3StorageServiceTests
{
    private readonly Mock<IS3Client> _mockS3Client;
    private readonly Mock<IOptionsMonitor<StorageOptions>> _mockOptions;
    private readonly S3StorageService _service;

    public S3StorageServiceTests()
    {
        _mockS3Client = new Mock<IS3Client>();
        _mockOptions = new Mock<IOptionsMonitor<StorageOptions>>();

        var options = new StorageOptions
        {
            S3 = new S3Options
            {
                ServiceUrl = "https://s3.amazonaws.com",
                BucketName = "ecg-signals-bucket"
            }
        };

        _mockOptions.Setup(m => m.CurrentValue).Returns(options);
        _service = new S3StorageService(_mockS3Client.Object, _mockOptions.Object);
    }

    [Fact]
    public async Task SaveAsync_ShouldForwardToS3Client()
    {
        // Arrange
        var path = "patient-001/ecg.json";
        var contentType = "application/json";
        using var stream = new MemoryStream();

        // Act
        await _service.SaveAsync(path, stream, contentType);

        // Assert
        _mockS3Client.Verify(x => x.UploadAsync(path, stream, contentType), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnStreamFromS3Client()
    {
        // Arrange
        var path = "signal.dat";
        var expectedStream = new MemoryStream();
        _mockS3Client.Setup(x => x.DownloadAsync(path)).ReturnsAsync(expectedStream);

        // Act
        var result = await _service.GetAsync(path);

        // Assert
        Assert.Equal(expectedStream, result);
        _mockS3Client.Verify(x => x.DownloadAsync(path), Times.Once);
    }

    [Fact]
    public void GetFullFilePath_ShouldConstructCorrectUrl()
    {
        // Arrange
        var path = "test.png";
        var expected = Path.Combine("https://s3.amazonaws.com", "ecg-signals-bucket", path);

        // Act
        var result = _service.GetFullFilePath(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task GetUrlAsync_ShouldReturnPresignedUrlFromClient()
    {
        // Arrange
        var path = "report.pdf";
        var expectedUrl = "https://signed-url.com";
        _mockS3Client.Setup(x => x.GetPresignedUrlAsync(path))
                     .ReturnsAsync((expectedUrl, DateTimeOffset.UtcNow.AddHours(1)));

        // Act
        (string? url, DateTimeOffset? expiresAt) = await _service.GetUrlAsync(path);

        // Assert
        Assert.Equal(expectedUrl, url);
    }
}