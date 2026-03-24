using Microsoft.AspNetCore.Authorization;

namespace EcgVision.Infrastructure.Security;

public record ViewJobRequirement : IAuthorizationRequirement;