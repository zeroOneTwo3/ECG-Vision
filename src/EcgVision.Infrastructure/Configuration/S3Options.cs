using System.ComponentModel.DataAnnotations;

namespace EcgVision.Infrastructure.Configuration;

public class S3Options
{
    public const string SectionName = "Storage:S3";

    [Required(AllowEmptyStrings = false)]
    [Url]
    public string ServiceUrl { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [StringLength(63, MinimumLength = 3)] // S3 bucket naming standards
    public string BucketName { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string AccessKey { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string SecretKey { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Region { get; set; } = string.Empty;

    [Range(1, 10080)] // 1 minute to 1 week (max for presigned URLs)
    public int PresignedUrlDurationMinutes { get; set; } = 60;
}