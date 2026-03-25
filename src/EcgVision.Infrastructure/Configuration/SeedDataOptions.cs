using System.ComponentModel.DataAnnotations;

namespace EcgVision.Infrastructure.Configuration;

public class SeedDataOptions
{
    public const string SectionName = "SeedData";

    [Required, EmailAddress]
    public string AdminEmail { get; set; } = "admin@ecgvision.com";

    [Required, MinLength(8)]
    public string AdminPassword { get; set; } = string.Empty;
}