using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcgVision.Core.Domain.Entities;

/// <summary>
/// represents the Permanent Record of the data
/// </summary>
public class EcgSignal
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string PatientId { get; set; } = string.Empty;

    [ForeignKey("PatientId")]
    public virtual ApplicationUser? Patient { get; set; }

    public string? RawSignalPath { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation: One Signal can have many processing attempts (Jobs)
    public virtual ICollection<EcgJob> Jobs { get; set; } = new List<EcgJob>();
}