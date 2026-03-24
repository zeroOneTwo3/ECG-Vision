using System.Security.Claims;

using EcgVision.Core.Domain.Entities;
using EcgVision.Core.Domain.Enums;

using Microsoft.AspNetCore.Authorization;

namespace EcgVision.Infrastructure.Security;

public class ViewJobAuthorizationHandler : AuthorizationHandler<ViewJobRequirement, EcgJob>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ViewJobRequirement requirement,
        EcgJob job)
    {
        var user = context.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        // Clinical Staff is allowed
        if (user.IsInRole(UserRole.Administrator.ToString()) || user.IsInRole(UserRole.Doctor.ToString()))
        {
            context.Succeed(requirement);
            return;
        }

        // Check Ownership for Patients
        if (job != null && (job.UserId == userId || job.Signal?.PatientId == userId))
        {
            context.Succeed(requirement);
        }
    }
}