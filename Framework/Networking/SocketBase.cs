/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Logging;
using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace Framework.Networking;

public interface ISocket
{
    void Accept();
    bool Update();
    bool IsOpen();
    void CloseSocket();
}

public abstract class SocketBase : ISocket, IDisposable
{
    Socket _socket;
    IPEndPoint? _remoteIPEndPoint;

    SocketAsyncEventArgs receiveSocketAsyncEventArgsWithCallback;
    SocketAsyncEventArgs receiveSocketAsyncEventArgs;

    byte[]? _callbackBuffer;
    byte[]? _asyncBuffer;
    const int BufferSize = 0x4000;
    const int MaxQueuedWriteItems = 256;
    const int MaxQueuedWriteBytes = 4 * 1024 * 1024;
    readonly BoundedSocketWriteQueue _writeQueue;

    public delegate void SocketReadCallback(SocketAsyncEventArgs args);

    protected SocketBase(Socket socket)
    {
        _socket = socket;
        _remoteIPEndPoint = _socket.RemoteEndPoint as IPEndPoint;
        _writeQueue = new BoundedSocketWriteQueue(
            (data, cancellationToken) => _socket.SendAsync(data, SocketFlags.None, cancellationToken),
            MaxQueuedWriteItems,
            MaxQueuedWriteBytes,
            HandleWriteQueueFailure);

        _callbackBuffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        _asyncBuffer = ArrayPool<byte>.Shared.Rent(BufferSize);

        receiveSocketAsyncEventArgsWithCallback = new SocketAsyncEventArgs();
        receiveSocketAsyncEventArgsWithCallback.SetBuffer(_callbackBuffer, 0, BufferSize);

        receiveSocketAsyncEventArgs = new SocketAsyncEventArgs();
        receiveSocketAsyncEventArgs.SetBuffer(_asyncBuffer, 0, BufferSize);
        receiveSocketAsyncEventArgs.Completed += (sender, args) => ProcessReadAsync(args);
    }

    public virtual void Dispose()
    {
        _writeQueue.Dispose();
        _socket.Dispose();

        if (_callbackBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(_callbackBuffer);
            _callbackBuffer = null;
        }

        if (_asyncBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(_asyncBuffer);
            _asyncBuffer = null;
        }

        receiveSocketAsyncEventArgsWithCallback?.Dispose();
        receiveSocketAsyncEventArgs?.Dispose();
    }

    public abstract void Accept();

    public virtual bool Update()
    {
        return IsOpen();
    }

    public IPEndPoint? GetRemoteIpAddress()
    {
        return _remoteIPEndPoint;
    }

    public void AsyncReadWithCallback(SocketReadCallback callback)
    {
        if (!IsOpen())
            return;

        receiveSocketAsyncEventArgsWithCallback.Completed += (sender, args) => callback(args);
        receiveSocketAsyncEventArgsWithCallback.SetBuffer(0, BufferSize);
        if (!_socket.ReceiveAsync(receiveSocketAsyncEventArgsWithCallback))
            callback(receiveSocketAsyncEventArgsWithCallback);
    }

    public void AsyncRead()
    {
        if (!IsOpen())
            return;

        receiveSocketAsyncEventArgs.SetBuffer(0, BufferSize);
        if (!_socket.ReceiveAsync(receiveSocketAsyncEventArgs))
            ProcessReadAsync(receiveSocketAsyncEventArgs);
    }

    void ProcessReadAsync(SocketAsyncEventArgs args)
    {
        if (args.SocketError != SocketError.Success)
        {
            CloseSocket();
            return;
        }

        if (args.BytesTransferred == 0)
        {
            CloseSocket();
            return;
        }

        ReadHandler(args);
    }

    public abstract void ReadHandler(SocketAsyncEventArgs args);

    public void AsyncWrite(byte[] data, Action? onSent = null)
    {
        if (!IsOpen())
            return;

        _writeQueue.TryEnqueue(data, onSent);
    }

    /// <summary>
    /// Writes one protocol-critical packet before returning. The modern client expects
    /// the realm/character-select handshake to be available as one ordered stream; the
    /// normal bounded queue remains the low-latency path for gameplay traffic.
    /// </summary>
    protected void SynchronousWrite(byte[] data, Action? onSent = null)
    {
        if (!IsOpen())
            return;

        try
        {
            int offset = 0;
            while (offset < data.Length)
            {
                int sent = _socket.Send(data, offset, data.Length - offset, SocketFlags.None);
                if (sent <= 0)
                    throw new SocketException((int)SocketError.ConnectionReset);
                offset += sent;
            }

            onSent?.Invoke();
        }
        catch (Exception exception)
        {
            HandleWriteQueueFailure(exception);
        }
    }

    private void HandleWriteQueueFailure(Exception exception)
    {
        Log.Print(LogType.Network, $"Socket write queue failed for {GetRemoteIpAddress()}: {exception.Message}");
        CloseSocket();
    }

    public void CloseSocket()
    {
        if (_socket == null || !_socket.Connected)
            return;

        try
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }
        catch (Exception ex)
        {
            Log.Print(LogType.Network, $"WorldSocket.CloseSocket: {GetRemoteIpAddress()} errored when shutting down socket: {ex.Message}");
        }

        OnClose();
    }

    public virtual void OnClose() { Dispose(); }

    public bool IsOpen() { return _socket.Connected; }

    public void SetNoDelay(bool enable)
    {
        _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, enable);
    }

    public bool GetNoDelay()
    {
        return _socket.NoDelay;
    }
}
