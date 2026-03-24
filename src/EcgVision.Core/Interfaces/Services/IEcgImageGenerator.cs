using EcgVision.Core.Domain.Enums;
using EcgVision.Core.Dtos;

namespace EcgVision.Core.Interfaces;

public interface IEcgImageGenerator
{
    byte[] GenerateLeadsPlot(EcgLeadsDto ecgLeads, JobType jobType);
}