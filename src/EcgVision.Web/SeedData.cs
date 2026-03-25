using EcgVision.Core.Domain.Entities;
using EcgVision.Core.Domain.Enums;
using EcgVision.Infrastructure.Configuration;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace EcgVision.Web;

public static class SeedData
{
    public static async Task SeedAllAsync(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<SeedDataOptions>>().Value;

        await SeedRolesAsync(serviceProvider);
        await SeedAdminUserAsync(serviceProvider, options);
    }

    private static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var roles = Enum.GetNames<UserRole>();

        foreach (var role in roles)
        {
            if (role == UserRole.None.ToString()) continue;
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedAdminUserAsync(IServiceProvider serviceProvider, SeedDataOptions options)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(options.AdminEmail);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = options.AdminEmail,
                Email = options.AdminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, options.AdminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, UserRole.Administrator.ToString());
            }
            else
            {
                // Optional: Log errors if password complexity isn't met
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Failed to seed Admin: {errors}");
            }
        }
    }
}