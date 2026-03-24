using System.ComponentModel.DataAnnotations;

using EcgVision.Core.Constants;
using EcgVision.Core.Domain.Enums;

namespace EcgVision.Core.Dtos;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(AppConstants.MaxPasswordLength, MinimumLength = AppConstants.MinPasswordLength)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public DateOnly DateOfBirth { get; set; }

    [Required]
    public Gender Gender { get; set; } = Gender.Unknown;

    [Required]
    public UserRole Role { get; set; } = UserRole.Patient;

}