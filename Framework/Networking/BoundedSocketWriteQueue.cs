using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Framework.Networking;

/// <summary>
/// Bounded FIFO writer for one socket. Exactly one worker owns the socket send
/// operation, and each buffer is drained completely before the next buffer starts.
/// </summary>
public sealed class BoundedSocketWriteQueue : IDisposable
{
    private readonly Channel<WriteItem> _queue;
    private readonly Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask<int>> _send;
    private readonly Action<Exception>? _failureCallback;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly object _stateLock = new();
    private TaskCompletionSource<object?> _idle = CompletedIdle();
    private int _pendingItems;
    private long _pendingBytes;
    private int _disposed;
    private int _failureReported;

    public BoundedSocketWriteQueue(
        Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask<int>> send,
        int maxItems,
        int maxBytes,
        Action<Exception>? failureCallback = null)
    {
        ArgumentNullException.ThrowIfNull(send);
        if (maxItems <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxItems));
        if (maxBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxBytes));

        _send = send;
        _failureCallback = failureCallback;
        _queue = Channel.CreateBounded<WriteItem>(new BoundedChannelOptions(maxItems)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });
        MaxItems = maxItems;
        MaxBytes = maxBytes;
        WriterTask = RunAsync();
    }

    public int MaxItems { get; }
    public int MaxBytes { get; }
    internal Task WriterTask { get; }

    public bool TryEnqueue(byte[] data, Action? onSent = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0)
            return true;

        Exception? rejection = null;
        lock (_stateLock)
        {
            if (_disposed != 0)
                rejection = new ObjectDisposedException(nameof(BoundedSocketWriteQueue));
            else if (_pendingItems >= MaxItems || _pendingBytes + data.Length > MaxBytes)
                rejection = new InvalidOperationException("The socket write queue is full.");
            else if (!_queue.Writer.TryWrite(new WriteItem(data, onSent)))
                rejection = new InvalidOperationException("The socket write queue is closed.");
            else
            {
                if (_pendingItems++ == 0)
                    _idle = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pendingBytes += data.Length;
                return true;
            }
        }

        ReportFailure(rejection);
        return false;
    }

    public Task WaitForIdleAsync(CancellationToken cancellationToken = default)
    {
        return _idle.Task.WaitAsync(cancellationToken);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _queue.Writer.TryComplete();
        _cancellation.Cancel();
        CompleteOutstanding();
    }

    private async Task RunAsync()
    {
        try
        {
            while (await _queue.Reader.WaitToReadAsync(_cancellation.Token).ConfigureAwait(false))
            {
                while (_queue.Reader.TryRead(out var item))
                {
                    var data = item.Data;
                    try
                    {
                        int offset = 0;
                        while (offset < data.Length)
                        {
                            int sent = await _send(data.AsMemory(offset), _cancellation.Token).ConfigureAwait(false);
                            if (sent <= 0)
                                throw new InvalidOperationException("The socket returned zero bytes for a non-empty send.");
                            offset += sent;
                        }

                        item.OnSent?.Invoke();
                    }
                    finally
                    {
                        MarkCompleted(data.Length);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
        {
            CompleteOutstanding();
        }
        catch (Exception ex)
        {
            Fail(ex);
        }
    }

    private void MarkCompleted(int bytes)
    {
        lock (_stateLock)
        {
            _pendingItems--;
            _pendingBytes -= bytes;
            if (_pendingItems == 0)
                _idle.TrySetResult(null);
        }
    }

    private void CompleteOutstanding(Exception? exception = null)
    {
        lock (_stateLock)
        {
            _pendingItems = 0;
            _pendingBytes = 0;
            if (exception == null)
                _idle.TrySetResult(null);
            else
                _idle.TrySetException(exception);
        }
    }

    private void ReportFailure(Exception? exception)
    {
        if (exception != null && Interlocked.Exchange(ref _failureReported, 1) == 0)
            _failureCallback?.Invoke(exception);
    }

    private void Fail(Exception exception)
    {
        Interlocked.Exchange(ref _disposed, 1);
        _queue.Writer.TryComplete(exception);
        _cancellation.Cancel();
        CompleteOutstanding(exception);
        ReportFailure(exception);
    }

    private static TaskCompletionSource<object?> CompletedIdle()
    {
        var source = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        source.SetResult(null);
        return source;
    }

    private sealed record WriteItem(byte[] Data, Action? OnSent);
}
