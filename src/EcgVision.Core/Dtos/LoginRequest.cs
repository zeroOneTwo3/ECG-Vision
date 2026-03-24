using System.ComponentModel.DataAnnotations;

using EcgVision.Core.Constants;

namespace EcgVision.Core.Dtos;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(AppConstants.MinPasswordLength)]
    [MaxLength(AppConstants.MaxPasswordLength)]
    public string Password { get; set; } = string.Empty;
}