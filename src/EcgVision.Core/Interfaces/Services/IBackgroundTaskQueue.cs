namespace EcgVision.Core.Interfaces;

public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(Guid jobId);
    ValueTask<Guid> DequeueAsync(CancellationToken ct);
}