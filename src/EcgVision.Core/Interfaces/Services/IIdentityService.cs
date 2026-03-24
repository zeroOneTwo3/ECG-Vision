using EcgVision.Core.Domain.Entities;
using EcgVision.Core.Dtos;
using EcgVision.Core.Dtos.Identity;

using Microsoft.AspNetCore.Identity;

namespace EcgVision.Core.Interfaces.Services;

public interface IIdentityService
{
    Task<string> GenerateJwtTokenAsync(ApplicationUser user);
    Task<AuthResponse?> AuthenticateAsync(string email, string password);
    Task<IdentityResult> RegisterUserAsync(RegisterRequest model);
}
