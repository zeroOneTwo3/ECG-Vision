using EcgVision.Core.Domain.Entities;

namespace EcgVision.Core.Dtos.Tasks;

public record GeneratePlotTask(EcgJob Job) : EcgTask(Job);
