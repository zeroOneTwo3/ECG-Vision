using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using EcgVision.Core.Domain.Entities;
using EcgVision.Core.Dtos;
using EcgVision.Core.Dtos.Identity;
using EcgVision.Core.Interfaces.Services;
using EcgVision.Infrastructure.Configuration;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EcgVision.Infrastructure.Services;

public class IdentityService(
    UserManager<ApplicationUser> userManager,
    IOptions<JwtOptions> jwtOptions) : IIdentityService
{
    public async Task<AuthResponse?> AuthenticateAsync(string email, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
            return null;

        var isPasswordValid = await userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid)
            return null;

        var expiresAt = DateTime.UtcNow.AddSeconds(jwtOptions.Value.ExpirationSeconds);
        var token = await GenerateJwtTokenAsync(user);

        return new AuthResponse(token, expiresAt);
    }

    public async Task<IdentityResult> RegisterUserAsync(RegisterRequest model)
    {
        var user = new ApplicationUser
        {
            FullName = model.Name,
            UserName = GenerateUsername(model.Name), // Keep this logic private in the service
            Email = model.Email,
            DateOfBirth = model.DateOfBirth,
            Gender = model.Gender
        };

        var result = await userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, model.Role.ToString());
        }

        return result;
    }
    public async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, user.UserName ?? "")
        };

        // Add roles to claims
        var userRoles = await userManager.GetRolesAsync(user);
        claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.Key!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddSeconds(jwtOptions.Value.ExpirationSeconds),
            SigningCredentials = creds,
            Issuer = jwtOptions.Value.Issuer,
            Audience = jwtOptions.Value.Audience
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public static string GenerateUsername(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;

        // 1. Split by spaces and remove empty entries
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2) return parts[0].ToLower();

        // 2. Take the first part (First Name) and the first char of the last part (Last Name)
        string firstName = parts[0].ToLower();
        char lastInitial = char.ToLower(parts[^1][0]); // [^1] is C# "Index from end"

        // 3. Combine with an underscore
        return $"{firstName}_{lastInitial}";
    }
}
