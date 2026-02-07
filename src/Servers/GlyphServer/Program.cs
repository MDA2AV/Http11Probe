using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Glyph11;
using Glyph11.Parser.Hardened;
using Glyph11.Protocol;
using Glyph11.Validation;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 5098;

var listener = new TcpListener(IPAddress.Loopback, port);
listener.Start();

Console.WriteLine($"GlyphServer listening on http://localhost:{port}");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        var client = await listener.AcceptTcpClientAsync(cts.Token);
        _ = HandleClientAsync(client, cts.Token);
    }
}
catch (OperationCanceledException) { }

listener.Stop();
Console.WriteLine("Server stopped.");

static async Task HandleClientAsync(TcpClient client, CancellationToken ct)
{
    using (client)
    await using (var stream = client.GetStream())
    {
        var limits = ParserLimits.Default;
        var reader = PipeReader.Create(stream);
        using var request = new BinaryRequest();

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var result = await reader.ReadAsync(ct);
                var buffer = result.Buffer;

                if (result.IsCompleted && buffer.IsEmpty)
                    break;

                var sequence = buffer;

                try
                {
                    if (!HardenedParser.TryExtractFullHeader(ref sequence, request, in limits, out var bytesRead))
                    {
                        if (buffer.Length > limits.MaxTotalHeaderBytes)
                        {
                            reader.AdvanceTo(buffer.End);
                            await stream.WriteAsync(MakeErrorResponse(431, "Request Header Fields Too Large"), ct);
                            break;
                        }

                        // Tell the pipe: consumed nothing, examined everything
                        reader.AdvanceTo(buffer.Start, buffer.End);

                        if (result.IsCompleted)
                            break;

                        continue;
                    }

                    // Post-parse semantic validation (must happen before AdvanceTo — request
                    // holds ReadOnlyMemory slices into the pipe's buffer)
                    if (RequestSemantics.HasTransferEncodingWithContentLength(request) ||
                        RequestSemantics.HasConflictingContentLength(request) ||
                        RequestSemantics.HasConflictingCommaSeparatedContentLength(request) ||
                        RequestSemantics.HasInvalidContentLengthFormat(request) ||
                        RequestSemantics.HasContentLengthWithLeadingZeros(request) ||
                        RequestSemantics.HasInvalidHostHeaderCount(request) ||
                        RequestSemantics.HasInvalidTransferEncoding(request) ||
                        RequestSemantics.HasDotSegments(request) ||
                        RequestSemantics.HasFragmentInRequestTarget(request) ||
                        RequestSemantics.HasBackslashInPath(request) ||
                        RequestSemantics.HasDoubleEncoding(request) ||
                        RequestSemantics.HasEncodedNullByte(request) ||
                        RequestSemantics.HasOverlongUtf8(request))
                    {
                        reader.AdvanceTo(buffer.End);
                        await stream.WriteAsync(MakeErrorResponse(400, "Bad Request"), ct);
                        break;
                    }

                    // Extract strings while buffer is still valid
                    var method = Encoding.ASCII.GetString(request.Method.Span);
                    var path = Encoding.ASCII.GetString(request.Path.Span);

                    // Advance past consumed bytes, then respond
                    reader.AdvanceTo(buffer.GetPosition(bytesRead));

                    var responseBytes = BuildResponse(method, path);
                    await stream.WriteAsync(responseBytes, ct);

                    request.Clear();
                }
                catch (HttpParseException ex)
                {
                    var (code, reason) = ex.IsLimitViolation
                        ? (431, "Request Header Fields Too Large")
                        : (400, "Bad Request");
                    reader.AdvanceTo(buffer.End);
                    await stream.WriteAsync(MakeErrorResponse(code, reason), ct);
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
        finally
        {
            await reader.CompleteAsync();
        }
    }
}

static byte[] BuildResponse(string method, string path)
{
    var body = $"Hello from GlyphServer\r\nMethod: {method}\r\nPath: {path}\r\n";
    return MakeResponse(200, "OK", body);
}

static byte[] MakeResponse(int status, string reason, string body)
{
    var bodyBytes = Encoding.UTF8.GetBytes(body);
    var header = $"HTTP/1.1 {status} {reason}\r\nContent-Type: text/plain\r\nContent-Length: {bodyBytes.Length}\r\nConnection: keep-alive\r\n\r\n";
    var headerBytes = Encoding.ASCII.GetBytes(header);

    var result = new byte[headerBytes.Length + bodyBytes.Length];
    Buffer.BlockCopy(headerBytes, 0, result, 0, headerBytes.Length);
    Buffer.BlockCopy(bodyBytes, 0, result, headerBytes.Length, bodyBytes.Length);
    return result;
}

static byte[] MakeErrorResponse(int status, string reason)
{
    return MakeResponse(status, reason, $"{status} {reason}\r\n");
}
