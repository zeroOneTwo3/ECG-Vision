using System.ComponentModel.DataAnnotations;

namespace EcgVision.Infrastructure.Configuration;

public class LocalStorageOptions
{
    [Required(AllowEmptyStrings = false)]
    [RegularExpression(@"^[^<>:\""|?*]+$", ErrorMessage = "Invalid characters in path.")]
    public string TempRoot { get; set; } = "temp_uploads";

    [Required(AllowEmptyStrings = false)]
    public string LandingZone { get; set; } = "landing-zone";

    public bool CreateIfNotExists { get; set; } = true;

    [Range(1, 24)] // 1 hour to 1 day
    public int StorageDurationHours { get; set; } = 6;
}