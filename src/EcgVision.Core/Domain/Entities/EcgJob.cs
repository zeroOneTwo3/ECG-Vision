using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using EcgVision.Core.Domain.Enums;

namespace EcgVision.Core.Domain.Entities;


/// <summary>
/// represents the Processing Lifecycle
/// </summary>
public class EcgJob
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SignalId { get; set; }
    public virtual EcgSignal? Signal { get; set; }

    // Who uploaded / intiated this job
    public string? UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }

    [Required]
    public JobType JobType { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    // Status: Pending, Parsing, JsonUploaded, GeneratingImage, Completed, Failed
    [Required]
    public EcgJobStatus Status { get; set; } = EcgJobStatus.None;

    public int ProgressPercentage { get; set; } = 0;

    public string? ErrorMessage { get; set; }

    // S3 Storage Keys (Paths)
    public string? JsonPath { get; set; }
    public string? ImagePath { get; set; }

    // Concurrency token to prevent multiple workers from grabbing the same job
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}