using EcgVision.Core.Dtos.Tasks;

namespace EcgVision.Core.Interfaces.Services;

public interface IEcgTaskHandler<in TTask> where TTask : EcgTask
{
    Task HandleAsync(TTask task, CancellationToken ct);
}
