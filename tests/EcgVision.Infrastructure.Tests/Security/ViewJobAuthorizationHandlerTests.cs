using System.Security.Claims;

using EcgVision.Core.Domain.Entities;
using EcgVision.Infrastructure.Security;

using Microsoft.AspNetCore.Authorization;

namespace EcgVision.Infrastructure.Tests.Security;

public class ViewJobAuthorizationHandlerTests
{
    private readonly ViewJobAuthorizationHandler _handler;
    private readonly ViewJobRequirement _requirement;

    public ViewJobAuthorizationHandlerTests()
    {
        _handler = new ViewJobAuthorizationHandler();
        _requirement = new ViewJobRequirement();
    }

    [Theory]
    [InlineData("Administrator")]
    [InlineData("Doctor")]
    public async Task HandleAsync_ShouldSucceed_WhenUserIsClinicalStaff(string role)
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, role)
        }, "TestAuth"));

        var job = new EcgJob { Id = Guid.NewGuid() };
        var context = new AuthorizationHandlerContext(new[] { _requirement }, user, job);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenUserIsJobOwner()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, "Patient")
        }, "TestAuth"));

        var job = new EcgJob { UserId = userId };
        var context = new AuthorizationHandlerContext(new[] { _requirement }, user, job);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_ShouldFail_WhenUserIsNotOwnerAndNotStaff()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "other-user"),
            new Claim(ClaimTypes.Role, "Patient")
        }, "TestAuth"));

        var job = new EcgJob { UserId = "original-owner-id" };
        var context = new AuthorizationHandlerContext(new[] { _requirement }, user, job);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenUserIsPatientLinkedToSignal()
    {
        // Arrange
        var patientId = "patient-123";
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, patientId)
        }, "TestAuth"));

        var job = new EcgJob
        {
            UserId = "system-process", // Job created by system, not patient
            Signal = new EcgSignal { PatientId = patientId }
        };

        var context = new AuthorizationHandlerContext(new[] { _requirement }, user, job);

        // Act
        await _handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }
}