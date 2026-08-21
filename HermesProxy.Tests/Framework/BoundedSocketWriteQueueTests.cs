using Framework.Networking;
using System.Collections.Concurrent;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace HermesProxy.Tests.Framework;

public class BoundedSocketWriteQueueTests
{
    [Fact]
    public async Task Queue_PreservesFifoOrder()
    {
        var sent = new ConcurrentQueue<string>();
        using var queue = new BoundedSocketWriteQueue(
            (data, _) =>
            {
                sent.Enqueue(Encoding.UTF8.GetString(data.Span));
                return ValueTask.FromResult(data.Length);
            },
            maxItems: 8,
            maxBytes: 128);

        Assert.True(queue.TryEnqueue(Encoding.UTF8.GetBytes("first")));
        Assert.True(queue.TryEnqueue(Encoding.UTF8.GetBytes("second")));
        Assert.True(queue.TryEnqueue(Encoding.UTF8.GetBytes("third")));

        await queue.WaitForIdleAsync();

        Assert.Equal(new[] { "first", "second", "third" }, sent.ToArray());
    }

    [Fact]
    public async Task Queue_CompletesPartialSendsBeforeAdvancing()
    {
        var sent = new StringBuilder();
        using var queue = new BoundedSocketWriteQueue(
            (data, _) =>
            {
                sent.Append(Encoding.UTF8.GetString(data.Span[..Math.Min(2, data.Length)]));
                return ValueTask.FromResult(Math.Min(2, data.Length));
            },
            maxItems: 4,
            maxBytes: 128);

        Assert.True(queue.TryEnqueue(Encoding.UTF8.GetBytes("abcdef")));
        await queue.WaitForIdleAsync();

        Assert.Equal("abcdef", sent.ToString());
    }

    [Fact]
    public async Task Queue_RejectsWhenBoundedCapacityIsFull()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var queue = new BoundedSocketWriteQueue(
            async (data, cancellationToken) =>
            {
                await release.Task.WaitAsync(cancellationToken);
                return data.Length;
            },
            maxItems: 1,
            maxBytes: 16);

        Assert.True(queue.TryEnqueue(new byte[8]));
        Assert.False(queue.TryEnqueue(new byte[8]));

        release.SetResult();
        await queue.WaitForIdleAsync();
    }
}
