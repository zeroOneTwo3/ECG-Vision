using Amazon.S3;
using Amazon.S3.Model;

using EcgVision.Infrastructure.Configuration;
using EcgVision.Infrastructure.ExternalServices;

using Microsoft.Extensions.Options;

using Moq;

namespace EcgVision.Infrastructure.Tests.ExternalServices;

public class S3ClientTests
{
    private readonly Mock<IAmazonS3> _mockS3;
    private readonly Mock<IOptionsMonitor<S3Options>> _mockOptions;
    private readonly S3Client _s3Client;
    private const string TestBucket = "ecg-vision-test-bucket";

    public S3ClientTests()
    {
        _mockS3 = new Mock<IAmazonS3>();
        _mockOptions = new Mock<IOptionsMonitor<S3Options>>();

        // Setup the Options to return our test bucket name
        _mockOptions.Setup(m => m.CurrentValue).Returns(new S3Options { BucketName = TestBucket });

        _s3Client = new S3Client(_mockS3.Object, _mockOptions.Object);
    }

    [Fact]
    public async Task UploadAsync_ShouldCallPutObjectWithCorrectParameters()
    {
        // Arrange
        var key = "signals/test.dat";
        var contentType = "application/octet-stream";
        using var stream = new MemoryStream("fake data"u8.ToArray());

        // Act
        await _s3Client.UploadAsync(key, stream, contentType);

        // Assert
        _mockS3.Verify(x => x.PutObjectAsync(
            It.Is<PutObjectRequest>(r =>
                r.BucketName == TestBucket &&
                r.Key == key &&
                r.ContentType == contentType),
            It.IsAny<CancellationToken>()),
        Times.Once);
    }

    [Fact]
    public async Task DownloadAsync_ShouldReturnStream_WhenKeyExists()
    {
        // Arrange
        var key = "results/plot.png";
        var expectedStream = new MemoryStream("image bytes"u8.ToArray());
        var response = new GetObjectResponse { ResponseStream = expectedStream };

        _mockS3.Setup(x => x.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(response);

        // Act
        var resultStream = await _s3Client.DownloadAsync(key);

        // Assert
        Assert.Equal(expectedStream, resultStream);
        _mockS3.Verify(x => x.GetObjectAsync(
            It.Is<GetObjectRequest>(r => r.Key == key && r.BucketName == TestBucket),
            It.IsAny<CancellationToken>()),
        Times.Once);
    }

    [Fact]
    public async Task GetPresignedUrlAsync_ShouldCallGetPreSignedURLWithExpiration()
    {
        // Arrange
        var key = "downloads/report.pdf";
        var expectedUrl = "https://s3.aws.com/presigned-url";

        _mockS3.Setup(x => x.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()))
               .Returns(expectedUrl);

        // Act
        (string url, DateTimeOffset expiresAt) = await _s3Client.GetPresignedUrlAsync(key);

        // Assert
        Assert.Equal(expectedUrl, url);
        _mockS3.Verify(x => x.GetPreSignedURL(
            It.Is<GetPreSignedUrlRequest>(r =>
                r.Key == key &&
                r.BucketName == TestBucket &&
                r.Expires > DateTime.UtcNow)),
        Times.Once);
    }
}