using EcgVision.Core.Dtos;

namespace EcgVision.Core.Interfaces;

public interface IEcgSignalParser
{
    Task<EcgLeadsDto> ExtractLeadsAsync(string recordPath, int leadCount = 4);
}