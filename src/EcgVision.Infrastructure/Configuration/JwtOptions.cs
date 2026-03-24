using System.ComponentModel.DataAnnotations;

namespace EcgVision.Infrastructure.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required, MinLength(32, ErrorMessage = "JWT Key must be at least 32 characters.")]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(60, 86400, ErrorMessage = "Expiration must be between 1 minute and 1 day.")]
    public int ExpirationSeconds { get; set; } = 3600;
}
