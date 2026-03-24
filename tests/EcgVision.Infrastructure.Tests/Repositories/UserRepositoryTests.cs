using EcgVision.Core.Domain.Entities;
using EcgVision.Infrastructure.Data.Repositories;

using Microsoft.AspNetCore.Identity;

using Moq;

namespace EcgVision.Infrastructure.Tests.Repositories;

public class UserRepositoryTests
{
    // Helper to create a Mocked UserManager
    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenIdIsValid()
    {
        // Arrange
        var mockUserManager = MockUserManager();
        var repo = new UserRepository(mockUserManager.Object);
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId.ToString(), Email = "dev@ecgvision.com" };

        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        // Act
        var result = await repo.GetByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Email, result.Email);
        mockUserManager.Verify(x => x.FindByIdAsync(userId.ToString()), Times.Once);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenEmailExists()
    {
        // Arrange
        var mockUserManager = MockUserManager();
        var repo = new UserRepository(mockUserManager.Object);
        var email = "tester@ecgvision.ru";
        var user = new ApplicationUser { Email = email };

        mockUserManager.Setup(x => x.FindByEmailAsync(email))
            .ReturnsAsync(user);

        // Act
        var result = await repo.GetByEmailAsync(email);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
    }
}