using EcgVision.Core.Domain.Entities;
using EcgVision.Core.Interfaces.Repositories;

using Microsoft.AspNetCore.Identity;

namespace EcgVision.Infrastructure.Data.Repositories;

public class UserRepository(UserManager<ApplicationUser> userManager) : IUserRepository
{
    public async Task<ApplicationUser?> GetByIdAsync(Guid id)
    {
        return await userManager.FindByIdAsync(id.ToString());
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        return await userManager.FindByEmailAsync(email);
    }
}
