using EcgVision.Core.Domain.Enums;

using Microsoft.AspNetCore.Identity;

namespace EcgVision.Web;

public static class SeedData
{
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Get all names from your Role enum
        // ["None", "Patient", "Doctor", "Administrator"]
        var roles = Enum.GetNames<UserRole>();

        foreach (var role in roles)
        {
            // We skip "None" if you don't want it as a functional database role
            if (role == UserRole.None.ToString()) continue;

            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}