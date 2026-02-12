using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Glyph11;
using Glyph11.Parser.Hardened;
using Glyph11.Protocol;
using Glyph11.Validation;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 8080;

var listener = new TcpListener(IPAddress.Any, port);
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
                // ── Phase 1: parse header ──────────────────────────
                // Loop until we have a complete header. Do NOT advance
                // the pipe yet — request holds ReadOnlyMemory slices
                // into the pipe buffer.
                ReadOnlySequence<byte> headerBuffer;
                int headerByteCount;
                while (true)
                {
                    request.Clear();
                    var result = await reader.ReadAsync(ct);
                    var buffer = result.Buffer;

                    if (result.IsCompleted && buffer.IsEmpty)
                    {
                        await reader.CompleteAsync();
                        return;
                    }

                    var sequence = buffer;
                    try
                    {
                        // TODO FOR SINGLE SEQUENCE THERE ARE NO ALLOCATIONS, FOR MULTI SEGMENT THERE ARE, THAT INTERFERES THE BEHAVIOR
                        // TODO MEANING WE CANT ADVANCE FOR SINGLE SEGMENT CASE
                        
                        if (HardenedParser.TryExtractFullHeader(ref sequence, request, in limits, out var bytesRead))
                        {
                            headerByteCount = bytesRead + 1;
                            headerBuffer = buffer;
                            break;
                        }

                        if (buffer.Length > limits.MaxTotalHeaderBytes)
                        {
                            reader.AdvanceTo(buffer.End);
                            await stream.WriteAsync(MakeErrorResponse(431, "Request Header Fields Too Large"), ct);
                            await reader.CompleteAsync();
                            return;
                        }

                        reader.AdvanceTo(buffer.Start, buffer.End);

                        if (result.IsCompleted)
                        {
                            await reader.CompleteAsync();
                            return;
                        }
                    }
                    catch (HttpParseException ex)
                    {
                        var code = ex.StatusCode;
                        var reason = code switch
                        {
                            431 => "Request Header Fields Too Large",
                            _ => "Bad Request"
                        };
                        reader.AdvanceTo(buffer.End);
                        await stream.WriteAsync(MakeErrorResponse(code, reason), ct);
                        await reader.CompleteAsync();
                        return;
                    }
                }

                // ── Phase 2: semantic validation ───────────────────
                // request slices still point into the live pipe buffer.
                if (RequestSemantics.HasTransferEncodingWithContentLength(request) ||
                    RequestSemantics.HasConflictingContentLength(request) ||
                    RequestSemantics.HasConflictingCommaSeparatedContentLength(request) ||
                    RequestSemantics.HasInvalidContentLengthFormat(request) ||
                    RequestSemantics.HasContentLengthWithLeadingZeros(request) ||
                    RequestSemantics.HasInvalidHostHeaderCount(request) ||
                    RequestSemantics.HasInvalidHostFormat(request) ||
                    RequestSemantics.HasInvalidTransferEncoding(request) ||
                    RequestSemantics.HasAsteriskFormWithoutOptions(request) ||
                    RequestSemantics.HasInvalidConnectRequest(request) ||
                    RequestSemantics.HasDotSegments(request) ||
                    RequestSemantics.HasFragmentInRequestTarget(request) ||
                    RequestSemantics.HasBackslashInPath(request) ||
                    RequestSemantics.HasDoubleEncoding(request) ||
                    RequestSemantics.HasEncodedNullByte(request) ||
                    RequestSemantics.HasOverlongUtf8(request))
                {
                    reader.AdvanceTo(headerBuffer.End);
                    await stream.WriteAsync(MakeErrorResponse(400, "Bad Request"), ct);
                    await reader.CompleteAsync();
                    return;
                }

                // ── Phase 3: extract values & detect framing ───────
                // Copy what we need out of the pipe buffer, then release it.
                var method = Encoding.ASCII.GetString(request.Method.Span);
                var path = Encoding.ASCII.GetString(request.Path.Span);
                var framing = HardenedParser.DetectBodyFraming(request);

                // Now safe to advance past the header bytes.
                reader.AdvanceTo(headerBuffer.GetPosition(headerByteCount));

                // ── Phase 4: consume body ──────────────────────────
                var bodyBytes = new MemoryStream();
                const int maxCapture = 4096;

                switch (framing.Framing)
                {
                    case BodyFraming.ContentLength:
                    {
                        long remaining = framing.ContentLength;
                        while (remaining > 0)
                        {
                            var result = await reader.ReadAsync(ct);
                            var buffer = result.Buffer;
                            long available = Math.Min(buffer.Length, remaining);

                            if (bodyBytes.Length < maxCapture)
                            {
                                var toCapture = (int)Math.Min(available, maxCapture - bodyBytes.Length);
                                foreach (var seg in buffer.Slice(0, toCapture))
                                    bodyBytes.Write(seg.Span);
                            }

                            remaining -= available;
                            reader.AdvanceTo(buffer.GetPosition(available));

                            if (result.IsCompleted && remaining > 0)
                            {
                                await reader.CompleteAsync();
                                return;
                            }
                        }
                        break;
                    }

                    case BodyFraming.Chunked:
                    {
                        var chunked = new ChunkedBodyStream();
                        while (true)
                        {
                            var result = await reader.ReadAsync(ct);
                            var buffer = result.Buffer;

                            ReadOnlySpan<byte> span;
                            byte[]? linearized = null;
                            if (buffer.IsSingleSegment)
                            {
                                span = buffer.FirstSpan;
                            }
                            else
                            {
                                linearized = new byte[buffer.Length];
                                buffer.CopyTo(linearized);
                                span = linearized;
                            }

                            bool done = false;
                            int totalConsumed = 0;
                            while (true)
                            {
                                var localSpan = span[totalConsumed..];
                                var cr = chunked.TryReadChunk(localSpan, out var consumed, out var dataOffset, out var dataLength);
                                totalConsumed += consumed;

                                if (cr == ChunkResult.Chunk && dataLength > 0 && bodyBytes.Length < maxCapture)
                                {
                                    var toCapture = Math.Min(dataLength, maxCapture - (int)bodyBytes.Length);
                                    bodyBytes.Write(localSpan.Slice(dataOffset, toCapture));
                                }

                                if (cr == ChunkResult.Completed)
                                {
                                    done = true;
                                    break;
                                }
                                if (cr == ChunkResult.NeedMoreData)
                                    break;
                                // ChunkResult.Chunk — loop to consume next chunk
                            }

                            reader.AdvanceTo(buffer.GetPosition(totalConsumed));

                            if (done)
                                break;

                            if (result.IsCompleted)
                            {
                                await reader.CompleteAsync();
                                return;
                            }
                        }
                        break;
                    }

                    case BodyFraming.None:
                    default:
                        break;
                }

                // ── Phase 5: send response ─────────────────────────
                var capturedBody = bodyBytes.Length > 0 ? Encoding.ASCII.GetString(bodyBytes.ToArray()) : null;
                var responseBytes = BuildResponse(method, path, capturedBody);
                await stream.WriteAsync(responseBytes, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (IOException) { }
        catch (HttpParseException ex)
        {
            var code = ex.StatusCode;
            var reason = code switch
            {
                431 => "Request Header Fields Too Large",
                _ => "Bad Request"
            };
            try { await stream.WriteAsync(MakeErrorResponse(code, reason), ct); } catch { }
        }
        finally
        {
            await reader.CompleteAsync();
        }
    }
}

static byte[] BuildResponse(string method, string path, string? echoBody)
{
    var body = method == "POST" && echoBody is not null
        ? echoBody
        : $"Hello from GlyphServer\r\nMethod: {method}\r\nPath: {path}\r\n";
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
