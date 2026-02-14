using System.Net;
using System.Net.Sockets;

namespace Http11Probe.Client;

public sealed class RawTcpClient : IAsyncDisposable
{
    private Socket? _socket;
    private readonly TimeSpan _connectTimeout;
    private readonly TimeSpan _readTimeout;

    public RawTcpClient(TimeSpan connectTimeout, TimeSpan readTimeout)
    {
        _connectTimeout = connectTimeout;
        _readTimeout = readTimeout;
    }

    public async Task<ConnectionState> ConnectAsync(string host, int port)
    {
        try
        {
            using var cts = new CancellationTokenSource(_connectTimeout);
            var addresses = await Dns.GetHostAddressesAsync(host, cts.Token);
            var ipv4 = Array.Find(addresses, a => a.AddressFamily == AddressFamily.InterNetwork);
            if (ipv4 is null)
                return ConnectionState.Error;

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };

            await _socket.ConnectAsync(new IPEndPoint(ipv4, port), cts.Token);
            return ConnectionState.Open;
        }
        catch (OperationCanceledException)
        {
            return ConnectionState.TimedOut;
        }
        catch
        {
            return ConnectionState.Error;
        }
    }

    public async Task SendAsync(ReadOnlyMemory<byte> data)
    {
        if (_socket is null)
            throw new InvalidOperationException("Not connected.");

        var sent = 0;
        while (sent < data.Length)
        {
            sent += await _socket.SendAsync(data[sent..], SocketFlags.None);
        }
    }

    public async Task<(byte[] Data, int Length, ConnectionState State, bool DrainCaughtData)> ReadResponseAsync()
    {
        if (_socket is null)
            return ([], 0, ConnectionState.Error, false);

        var buffer = new byte[65536];
        var totalRead = 0;

        using var cts = new CancellationTokenSource(_readTimeout);

        try
        {
            // Phase 1: Read until we have the complete headers (\r\n\r\n)
            while (totalRead < buffer.Length)
            {
                var read = await _socket.ReceiveAsync(
                    buffer.AsMemory(totalRead),
                    SocketFlags.None,
                    cts.Token);

                if (read == 0)
                    return (buffer, totalRead, ConnectionState.ClosedByServer, false);

                totalRead += read;

                if (FindHeaderTerminator(buffer.AsSpan(0, totalRead)) >= 0)
                    break;
            }

            // Phase 2: Wait briefly for the body to arrive, then drain
            await Task.Delay(100, cts.Token);
            var beforeDrain = totalRead;
            totalRead = await DrainAvailable(buffer, totalRead, cts.Token);
            var drainCaughtData = totalRead > beforeDrain;

            return (buffer, totalRead, ConnectionState.Open, drainCaughtData);
        }
        catch (OperationCanceledException)
        {
            return (buffer, totalRead, ConnectionState.TimedOut, false);
        }
        catch (SocketException)
        {
            return (buffer, totalRead, ConnectionState.ClosedByServer, false);
        }
        catch
        {
            return (buffer, totalRead, ConnectionState.Error, false);
        }
    }

    /// <summary>
    /// Non-blocking drain: reads whatever bytes are already in the socket buffer
    /// without waiting for more data to arrive.
    /// </summary>
    private async Task<int> DrainAvailable(byte[] buffer, int totalRead, CancellationToken ct)
    {
        if (_socket is null) return totalRead;

        while (totalRead < buffer.Length)
        {
            // Poll with zero timeout — returns true only if data is ready right now
            if (!_socket.Poll(0, SelectMode.SelectRead))
                break;

            var read = await _socket.ReceiveAsync(
                buffer.AsMemory(totalRead),
                SocketFlags.None,
                ct);

            if (read == 0) break; // peer closed
            totalRead += read;
        }

        return totalRead;
    }

    public ConnectionState CheckConnectionState()
    {
        if (_socket is null || !_socket.Connected)
            return ConnectionState.ClosedByServer;

        try
        {
            // Poll for readability with zero timeout — if readable and Receive would return 0, peer closed
            if (_socket.Poll(0, SelectMode.SelectRead))
            {
                var buf = new byte[1];
                var read = _socket.Receive(buf, SocketFlags.Peek);
                return read == 0 ? ConnectionState.ClosedByServer : ConnectionState.Open;
            }

            return ConnectionState.Open;
        }
        catch
        {
            return ConnectionState.ClosedByServer;
        }
    }

    private static int FindHeaderTerminator(ReadOnlySpan<byte> data)
    {
        ReadOnlySpan<byte> terminator = [0x0D, 0x0A, 0x0D, 0x0A];
        return data.IndexOf(terminator);
    }

    public async ValueTask DisposeAsync()
    {
        if (_socket is not null)
        {
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
                // Ignore — socket may already be closed
            }

            _socket.Dispose();
            _socket = null;
        }

        await ValueTask.CompletedTask;
    }
}
