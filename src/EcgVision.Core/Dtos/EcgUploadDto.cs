using EcgVision.Core.Domain.Enums;

namespace EcgVision.Core.Dtos;

public class EcgUploadDto
{
    public Guid PatientId { get; init; }
    public Guid UserId { get; init; }
    public JobType JobType { get; init; }
    public EcgFilesDto Files { get; init; } = default!;
}