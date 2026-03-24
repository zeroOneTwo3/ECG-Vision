using System.ComponentModel.DataAnnotations;

using EcgVision.Core.Domain.Enums;

namespace EcgVision.Infrastructure.Configuration;

public class StorageOptions
{
    public const string SectionName = "Storage";

    [Required]
    public LocalStorageOptions Local { get; set; } = new();

    [Required]
    public S3Options S3 { get; set; } = new();

    // Strategy Pattern settings
    public StorageLocation RawEcgLocation { get; set; } = StorageLocation.Local;
    public StorageLocation JsonEcgLocation { get; set; } = StorageLocation.S3;
    public StorageLocation ImageEcgLocation { get; set; } = StorageLocation.S3;
}
