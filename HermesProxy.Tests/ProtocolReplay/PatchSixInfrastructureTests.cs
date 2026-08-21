using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Framework.Networking;
using HermesProxy.Telemetry;
using Xunit;

namespace HermesProxy.Tests.ProtocolReplay;

public sealed class PatchSixInfrastructureTests
{
    [Fact]
    public async Task SocketWriter_PreservesFifoAcrossPartialSends()
    {
        var sent = new StringBuilder();
        using var queue = new BoundedSocketWriteQueue(
            (data, _) =>
            {
                var length = Math.Min(2, data.Length);
                sent.Append(Encoding.UTF8.GetString(data.Span[..length]));
                return ValueTask.FromResult(length);
            },
            maxItems: 4,
            maxBytes: 64);

        Assert.True(queue.TryEnqueue(Encoding.UTF8.GetBytes("first")));
        Assert.True(queue.TryEnqueue(Encoding.UTF8.GetBytes("second")));

        await queue.WaitForIdleAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);

        Assert.Equal("firstsecond", sent.ToString());
    }

    [Fact]
    public async Task SocketWriter_DisposalCancelsPendingWriteAndCompletesIdleWaiters()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var queue = new BoundedSocketWriteQueue(
            async (_, cancellationToken) =>
            {
                started.SetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    cancelled.SetResult();
                    throw;
                }

                return 0;
            },
            maxItems: 1,
            maxBytes: 16);

        Assert.True(queue.TryEnqueue(new byte[8]));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        queue.Dispose();

        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        await queue.WaitForIdleAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SocketWriter_PeerResetRejectsFutureWritesWithoutDuplicateFailure()
    {
        var failure = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var failures = 0;
        using var queue = new BoundedSocketWriteQueue(
            (_, _) => throw new SocketException((int)SocketError.ConnectionReset),
            maxItems: 2,
            maxBytes: 32,
            exception =>
            {
                Interlocked.Increment(ref failures);
                failure.TrySetResult(exception);
            });

        Assert.True(queue.TryEnqueue(new byte[8]));
        await failure.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);

        Assert.False(queue.TryEnqueue(new byte[8]));
        Assert.Equal(1, Volatile.Read(ref failures));
    }

    [Fact]
    public async Task SocketWriter_QueueOverflowIsExplicit()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var overflow = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var queue = new BoundedSocketWriteQueue(
            async (_, cancellationToken) =>
            {
                started.SetResult();
                await release.Task.WaitAsync(cancellationToken);
                return 1;
            },
            maxItems: 1,
            maxBytes: 16,
            exception => overflow.TrySetResult(exception));

        Assert.True(queue.TryEnqueue(new byte[8]));
        await started.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        Assert.False(queue.TryEnqueue(new byte[8]));
        var error = await overflow.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        Assert.Contains("full", error.Message, StringComparison.OrdinalIgnoreCase);

        release.SetResult();
        await queue.WaitForIdleAsync(TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
    }

    [Fact]
    public void Telemetry_AssignsRepairWhenExercisedLifecycleHasNoSample()
    {
        var snapshot = new TelemetrySnapshot(
            "patch-six",
            DateTimeOffset.UtcNow,
            new Dictionary<string, long> { ["lifecycle_exercised.Selection"] = 1 },
            new Dictionary<string, TelemetryLatencySummary>(),
            []);

        var task = Assert.Single(TelemetryTaskAssigner.Assign(snapshot));

        Assert.Equal("HERMES-WOTLK-LIFECYCLE-TELEMETRY", task.Id);
    }

    [Fact]
    public void Telemetry_ExpectedShutdownDoesNotAssignConnectionStability()
    {
        var snapshot = new TelemetrySnapshot(
            "patch-six",
            DateTimeOffset.UtcNow,
            new Dictionary<string, long> { ["connection_expected_shutdown"] = 1 },
            new Dictionary<string, TelemetryLatencySummary>(),
            []);

        Assert.DoesNotContain(
            TelemetryTaskAssigner.Assign(snapshot),
            task => task.Id == "HERMES-WOTLK-CONNECTION-STABILITY");
    }
}
