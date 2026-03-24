using System.Threading.Channels;

using EcgVision.Core.Interfaces;

namespace EcgVision.Infrastructure.Concurrency;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Guid> _queue = Channel.CreateBounded<Guid>(100);
    public async ValueTask QueueBackgroundWorkItemAsync(Guid jobId) => await _queue.Writer.WriteAsync(jobId);
    public async ValueTask<Guid> DequeueAsync(CancellationToken ct) => await _queue.Reader.ReadAsync(ct);
}