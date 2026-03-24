using EcgVision.Core.Domain.Entities;

namespace EcgVision.Core.Dtos.Tasks;

public record ExtractSignalTask(EcgJob Job, int LeadCount) : EcgTask(Job);
