using EcgVision.Infrastructure.Concurrency;

namespace EcgVision.Infrastructure.Tests.Concurrency;

public class BackgroundTaskQueueTests
{
    private readonly BackgroundTaskQueue _queue;

    public BackgroundTaskQueueTests()
    {
        _queue = new BackgroundTaskQueue();
    }

    [Fact]
    public async Task QueueAndDequeue_ShouldMaintainFifoOrder()
    {
        // Arrange
        var jobId1 = Guid.NewGuid();
        var jobId2 = Guid.NewGuid();

        // Act
        await _queue.QueueBackgroundWorkItemAsync(jobId1);
        await _queue.QueueBackgroundWorkItemAsync(jobId2);

        var result1 = await _queue.DequeueAsync(CancellationToken.None);
        var result2 = await _queue.DequeueAsync(CancellationToken.None);

        // Assert
        Assert.Equal(jobId1, result1);
        Assert.Equal(jobId2, result2);
    }

    [Fact]
    public async Task DequeueAsync_ShouldWait_UntilItemIsAdded()
    {
        // Arrange
        var jobId = Guid.NewGuid();

        // Act
        // We start the dequeue task first (it will "hang" until data arrives)
        var dequeueTask = _queue.DequeueAsync(CancellationToken.None).AsTask();

        // Wait a small bit to ensure it's actually waiting
        await Task.Delay(50);
        Assert.False(dequeueTask.IsCompleted);

        await _queue.QueueBackgroundWorkItemAsync(jobId);
        var result = await dequeueTask;

        // Assert
        Assert.Equal(jobId, result);
    }

    [Fact]
    public async Task DequeueAsync_ShouldThrow_WhenCanceled()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var dequeueTask = _queue.DequeueAsync(cts.Token).AsTask();

        // Cancel the operation immediately
        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await dequeueTask);
    }

    [Fact]
    public async Task Queue_ShouldNotBlock_UnderNormalLoad()
    {
        // Arrange
        var jobId = Guid.NewGuid();

        // Act
        var task = _queue.QueueBackgroundWorkItemAsync(jobId).AsTask();

        // Assert
        // In a non-full Bounded channel, WriteAsync completes immediately
        Assert.True(task.IsCompleted);
    }
}
