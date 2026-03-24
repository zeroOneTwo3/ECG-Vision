using EcgVision.Core.Domain.Entities;

namespace EcgVision.Core.Dtos.Tasks;

public record SaveJsonTask(EcgJob Job) : EcgTask(Job);
