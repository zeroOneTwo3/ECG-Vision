using EcgVision.Core.Domain.Enums;
using EcgVision.Core.Interfaces.Services;
using EcgVision.Infrastructure.Configuration;
using EcgVision.Infrastructure.FileManagement;

using Microsoft.Extensions.Options;

using Moq;

namespace EcgVision.Infrastructure.Tests.FileManagement;

public class StorageFactoryTests
{
    private readonly Mock<IStorageService> _mockLocalService;
    private readonly Mock<IStorageService> _mockS3Service;
    private readonly Mock<IOptions<StorageOptions>> _mockOptions;
    private readonly StorageFactory _factory;

    public StorageFactoryTests()
    {
        // 1. Setup Mock Services with different locations
        _mockLocalService = new Mock<IStorageService>();
        _mockLocalService.Setup(s => s.Location).Returns(StorageLocation.Local);

        _mockS3Service = new Mock<IStorageService>();
        _mockS3Service.Setup(s => s.Location).Returns(StorageLocation.S3);

        // Create the list of services the factory will choose from
        var services = new List<IStorageService> { _mockLocalService.Object, _mockS3Service.Object };

        // 2. Setup Options (Configuring where each DataContext should go)
        var options = new StorageOptions
        {
            JsonEcgLocation = StorageLocation.S3,
            ImageEcgLocation = StorageLocation.S3
            // Note: Raw is hardcoded to Local in your current Factory logic
        };

        _mockOptions = new Mock<IOptions<StorageOptions>>();
        _mockOptions.Setup(m => m.Value).Returns(options);

        _factory = new StorageFactory(services, _mockOptions.Object);
    }

    [Fact]
    public void GetService_ShouldReturnLocal_WhenContextIsRaw()
    {
        // Act
        var service = _factory.GetService(DataContext.Raw);

        // Assert
        Assert.Equal(StorageLocation.Local, service.Location);
        Assert.IsAssignableFrom<IStorageService>(service);
    }

    [Fact]
    public void GetService_ShouldReturnS3_WhenContextIsJson()
    {
        // Act
        var service = _factory.GetService(DataContext.Json);

        // Assert
        Assert.Equal(StorageLocation.S3, service.Location);
    }

    [Fact]
    public void GetService_ShouldReturnS3_WhenContextIsImage()
    {
        // Act
        var service = _factory.GetService(DataContext.Image);

        // Assert
        Assert.Equal(StorageLocation.S3, service.Location);
    }

    [Fact]
    public void GetService_ShouldThrow_WhenContextIsInvalid()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _factory.GetService((DataContext)999));
    }
}