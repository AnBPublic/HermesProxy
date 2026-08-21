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
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.Networking;

public abstract class SSLSocket : ISocket, IDisposable
{
    Socket _socket;
    internal SslStream _stream;
    IPEndPoint? _remoteEndPoint;
    byte[]? _receiveBuffer;
    readonly BoundedSocketWriteQueue _writeQueue;
    int _closed;

    protected SSLSocket(Socket socket)
    {
        _socket = socket;
        _remoteEndPoint = _socket.RemoteEndPoint as IPEndPoint;
        _receiveBuffer = new byte[ushort.MaxValue];

        _stream = new SslStream(new NetworkStream(socket), false);
        _writeQueue = new BoundedSocketWriteQueue(
            async (data, cancellationToken) =>
            {
                await _stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
                return data.Length;
            },
            maxItems: 256,
            maxBytes: 4 * 1024 * 1024,
            HandleWriteQueueFailure);
    }

    public virtual void Dispose()
    {
        _writeQueue.Dispose();
        _receiveBuffer = null!;
        _stream.Dispose();
    }

    public abstract void Accept();

    public virtual bool Update()
    {
        return _socket.Connected;
    }

    public IPEndPoint? GetRemoteIpEndPoint()
    {
        return _remoteEndPoint;
    }

    public async Task AsyncRead()
    {
        if (!IsOpen() || _receiveBuffer is null)
            return;

        try
        {
            var receiveBuffer = _receiveBuffer;
            var result = await _stream.ReadAsync(receiveBuffer, 0, receiveBuffer.Length);
            if (result == 0)
            {
                CloseSocket();
                return;
            }

            _ = ReadHandler(receiveBuffer, result);
        }
        catch (Exception ex)
        {
            Log.outException(ex);
        }
    }

    public async Task AsyncHandshake(X509Certificate2 certificate)
    {
        try
        {
            await _stream.AuthenticateAsServerAsync(certificate, false, SslProtocols.Tls12, false);
        }
        catch (Exception ex) when (ex is AuthenticationException || ex is System.IO.IOException)
        {
            // WoW retail opens BNet probe connections that arrive without a valid TLS
            // ClientHello (AuthenticationException) or close mid-handshake (IOException
            // "unexpected EOF"). Either way it's a probe — log a single line, no stack.
            Log.Print(LogType.Warn, $"TLS handshake failed for {GetRemoteIpEndPoint()}: {ex.Message}");
            CloseSocket();
            return;
        }
        catch (Exception ex)
        {
            Log.outException(ex);
            CloseSocket();
            return;
        }

        await AsyncRead();
    }

    public abstract Task ReadHandler(byte[] data, int receivedLength);

    public async Task AsyncWrite(byte[] data)
    {
        if (!IsOpen())
            return;

        if (!_writeQueue.TryEnqueue(data))
            return;

        try
        {
            await _writeQueue.WaitForIdleAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Socket shutdown cancels any queued write deterministically.
        }
        catch (Exception ex)
        {
            Log.outException(ex);
        }
    }

    public void CloseSocket()
    {
        if (Interlocked.Exchange(ref _closed, 1) != 0)
            return;

        _writeQueue.Dispose();
        try
        {
            if (_socket.Connected)
                _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }
        catch (Exception ex)
        {
            Log.Print(LogType.Network, $"WorldSocket.CloseSocket: {GetRemoteIpEndPoint()} errored when shutting down socket: {ex.Message}");
        }
    }

    private void HandleWriteQueueFailure(Exception exception)
    {
        Log.Print(LogType.Network, $"TLS socket write failed for {GetRemoteIpEndPoint()}: {exception.Message}");
        CloseSocket();
    }

    public virtual void OnClose() { Dispose(); }

    public bool IsOpen() { return _socket.Connected; }

    public void SetNoDelay(bool enable)
    {
        _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, enable);
    }
}
