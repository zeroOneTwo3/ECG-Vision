using EcgVision.Core.Domain.Enums;

using Microsoft.AspNetCore.Identity;

namespace EcgVision.Core.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public DateOnly DateOfBirth { get; set; }
    public Gender Gender { get; set; } = Gender.Unknown;
    public float Weight { get; set; }
    public string FullName { get; set; } = string.Empty;
}