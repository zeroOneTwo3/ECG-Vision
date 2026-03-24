using EcgVision.Core.Domain.Entities;
using EcgVision.Core.Domain.Enums;
using EcgVision.Infrastructure.Configuration;
using EcgVision.Infrastructure.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

using Moq;

namespace EcgVision.Infrastructure.Tests.Services;

public class IdentityServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
    private readonly IOptions<JwtOptions> _jwtOptions;
    private readonly IdentityService _service;

    public IdentityServiceTests()
    {
        _mockUserManager = MockUserManager();
        _jwtOptions = Options.Create(new JwtOptions
        {
            Key = "a_very_long_secret_key_for_testing_purposes_only",
            Issuer = "EcgVision",
            Audience = "EcgVisionUsers",
            ExpirationSeconds = 3600
        });

        _service = new IdentityService(_mockUserManager.Object, _jwtOptions);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnToken_WhenCredentialsAreValid()
    {
        // Arrange
        var email = "test@example.com";
        var password = "Password123!";
        var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = email, UserName = "test_u" };

        _mockUserManager.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, password)).ReturnsAsync(true);
        _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Patient" });

        // Act
        var result = await _service.AuthenticateAsync(email, password);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Token);
        _mockUserManager.Verify(x => x.CheckPasswordAsync(user, password), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenPasswordIsInvalid()
    {
        // Arrange
        var user = new ApplicationUser { Email = "test@example.com" };
        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
        _mockUserManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);

        // Act
        var result = await _service.AuthenticateAsync("test@example.com", "wrong-pass");

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(UserRole.Doctor)]
    [InlineData(UserRole.Administrator)]
    [InlineData(UserRole.Patient)]
    public async Task RegisterUserAsync_ShouldCreateUserAndAddRole_WhenSuccessful(UserRole role)
    {
        // Arrange
        var request = new Core.Dtos.RegisterRequest()
        {
            Name = "John Doe",
            Email = "john@example.com",
            Password = "Pass123!",
            DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-30)),
            Gender = Gender.Male,
            Role = role
        };

        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), role.ToString()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _service.RegisterUserAsync(request);

        // Assert
        Assert.True(result.Succeeded);
        _mockUserManager.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => u.Email == request.Email), It.IsAny<string>()), Times.Once);
        _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), role.ToString()), Times.Once);
    }

    [Theory]
    [InlineData("John Doe", "john_d")]
    [InlineData("Alice", "alice")]
    [InlineData("Bob Smith Jones", "bob_j")]
    [InlineData("  ", "")]
    public void GenerateUsername_ShouldReturnCorrectFormat(string input, string expected)
    {
        // Act
        var result = IdentityService.GenerateUsername(input);

        // Assert
        Assert.Equal(expected, result);
    }

    private static Mock<UserManager<ApplicationUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }
}