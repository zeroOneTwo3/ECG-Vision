using EcgVision.Core.Domain.Entities;

namespace EcgVision.Core.Dtos.Tasks;

public record SavePlotTask(EcgJob Job) : EcgTask(Job);
