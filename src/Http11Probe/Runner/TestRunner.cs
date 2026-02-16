using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using Http11Probe.Client;
using Http11Probe.Response;
using Http11Probe.TestCases;

namespace Http11Probe.Runner;

public sealed class TestRunner
{
    private readonly TestRunOptions _options;

    public TestRunner(TestRunOptions options)
    {
        _options = options;
    }

    public async Task<TestRunReport> RunAsync(IEnumerable<ITestCase> testCases, Action<TestResult>? onResult = null)
    {
        var results = new List<TestResult>();
        var totalSw = Stopwatch.StartNew();

        var context = new TestContext
        {
            Host = _options.Host,
            Port = _options.Port
        };

        foreach (var testCase in testCases)
        {
            if (_options.CategoryFilter.HasValue && testCase.Category != _options.CategoryFilter.Value
                || _options.TestIdFilter is { Count: > 0 } ids && !ids.Contains(testCase.Id))
            {
                var skip = new TestResult
                {
                    TestCase = testCase,
                    Verdict = TestVerdict.Skip,
                    ConnectionState = ConnectionState.Open,
                    Duration = TimeSpan.Zero
                };
                results.Add(skip);
                continue;
            }

            var result = testCase switch
            {
                SequenceTestCase seq => await RunSequenceAsync(seq, context),
                TestCase single => await RunSingleAsync(single, context),
                _ => throw new InvalidOperationException($"Unknown test case type: {testCase.GetType().Name}")
            };
            results.Add(result);
            onResult?.Invoke(result);
        }

        totalSw.Stop();

        return new TestRunReport
        {
            Results = results,
            TotalDuration = totalSw.Elapsed
        };
    }

    private async Task<TestResult> RunSingleAsync(TestCase testCase, TestContext context)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            await using var client = new RawTcpClient(_options.ConnectTimeout, _options.ReadTimeout);
            var connectState = await client.ConnectAsync(_options.Host, _options.Port);

            if (connectState != ConnectionState.Open)
            {
                return new TestResult
                {
                    TestCase = testCase,
                    Verdict = TestVerdict.Error,
                    ConnectionState = connectState,
                    ErrorMessage = $"Failed to connect: {connectState}",
                    Duration = sw.Elapsed
                };
            }

            // Send the primary payload
            var payload = testCase.PayloadFactory(context);
            var rawRequest = payload.Length > 8192
                ? Encoding.ASCII.GetString(payload, 0, 8192) + $"\n\n[Truncated — showing 8,192 of {payload.Length:N0} bytes]"
                : Encoding.ASCII.GetString(payload);
            await client.SendAsync(payload);

            // Read primary response
            var (data, length, readState, drainCaughtData) = await client.ReadResponseAsync();
            var response = ResponseParser.TryParse(data.AsSpan(), length);

            var connectionState = readState;

            if (connectionState == ConnectionState.Open)
            {
                // Brief pause then check if server closed the connection
                await Task.Delay(50);
                connectionState = client.CheckConnectionState();
            }

            var verdict = testCase.Expected.Evaluate(response, connectionState);
            var behavioralNote = testCase.BehavioralAnalyzer?.Invoke(response);

            return new TestResult
            {
                TestCase = testCase,
                Verdict = verdict,
                Response = response,
                ConnectionState = connectionState,
                BehavioralNote = behavioralNote,
                RawRequest = rawRequest,
                DrainCaughtData = drainCaughtData,
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestCase = testCase,
                Verdict = TestVerdict.Error,
                ConnectionState = ConnectionState.Error,
                ErrorMessage = ex.Message,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<TestResult> RunSequenceAsync(SequenceTestCase seq, TestContext context)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            await using var client = new RawTcpClient(_options.ConnectTimeout, _options.ReadTimeout);
            var connectState = await client.ConnectAsync(_options.Host, _options.Port);

            if (connectState != ConnectionState.Open)
            {
                return new TestResult
                {
                    TestCase = seq,
                    Verdict = TestVerdict.Error,
                    ConnectionState = connectState,
                    ErrorMessage = $"Failed to connect: {connectState}",
                    Duration = sw.Elapsed
                };
            }

            var stepResults = new List<StepResult>();
            var rawRequestParts = new List<string>();
            HttpResponse? lastResponse = null;
            var connectionState = ConnectionState.Open;
            var drainCaughtData = false;

            for (var i = 0; i < seq.Steps.Count; i++)
            {
                var step = seq.Steps[i];
                var label = step.Label ?? $"Step {i + 1}";

                if (connectionState != ConnectionState.Open)
                {
                    stepResults.Add(new StepResult
                    {
                        Label = label,
                        Executed = false,
                        ConnectionState = connectionState
                    });
                    rawRequestParts.Add($"── {label} ──\n[Not executed — connection closed]");
                    continue;
                }

                var parts = step.SendPartsFactory?.Invoke(context);
                if (parts is null)
                {
                    Func<TestContext, byte[]>? effectiveFactory = null;

                    if (step.DynamicPayloadFactory is not null)
                        effectiveFactory = ctx => step.DynamicPayloadFactory(ctx, stepResults);
                    else if (step.PayloadFactory is not null)
                        effectiveFactory = step.PayloadFactory;

                    if (effectiveFactory is null)
                        throw new InvalidOperationException($"Sequence step '{label}' has no payload factory.");

                    parts = [new SequenceSendPart { PayloadFactory = effectiveFactory }];
                }

                var partPayloads = new List<(byte[] Bytes, TimeSpan DelayAfter, string? PartLabel)>();
                foreach (var part in parts)
                {
                    var bytes = part.PayloadFactory(context);
                    partPayloads.Add((bytes, part.DelayAfter, part.Label));
                }

                string rawReq;
                if (partPayloads.Count == 1 && partPayloads[0].DelayAfter == TimeSpan.Zero && string.IsNullOrWhiteSpace(partPayloads[0].PartLabel))
                {
                    var payload = partPayloads[0].Bytes;
                    rawReq = payload.Length > 8192
                        ? Encoding.ASCII.GetString(payload, 0, 8192) + "\n\n[Truncated]"
                        : Encoding.ASCII.GetString(payload);
                }
                else
                {
                    var sb = new StringBuilder();
                    for (var pi = 0; pi < partPayloads.Count; pi++)
                    {
                        var (bytes, delayAfter, partLabel) = partPayloads[pi];
                        var partHeader = partLabel is null ? $"Part {pi + 1}" : $"Part {pi + 1} — {partLabel}";
                        var rawPart = bytes.Length > 8192
                            ? Encoding.ASCII.GetString(bytes, 0, 8192) + "\n\n[Truncated]"
                            : Encoding.ASCII.GetString(bytes);

                        sb.AppendLine($"[{partHeader}]");
                        sb.AppendLine(rawPart);

                        if (delayAfter > TimeSpan.Zero)
                            sb.AppendLine($"[Pause {delayAfter.TotalMilliseconds:0} ms]");
                    }

                    rawReq = sb.ToString().TrimEnd('\r', '\n');
                }
                rawRequestParts.Add($"── {label} ──\n{rawReq}");

                foreach (var (bytes, delayAfter, _) in partPayloads)
                {
                    try
                    {
                        await client.SendAsync(bytes);
                    }
                    catch (SocketException)
                    {
                        connectionState = ConnectionState.ClosedByServer;
                        break;
                    }
                    catch
                    {
                        connectionState = ConnectionState.Error;
                        break;
                    }

                    if (delayAfter > TimeSpan.Zero)
                    {
                        await Task.Delay(delayAfter);

                        if (client.CheckConnectionState() != ConnectionState.Open)
                        {
                            connectionState = ConnectionState.ClosedByServer;
                            break;
                        }
                    }
                }

                var (data, length, readState, drain) = await client.ReadResponseAsync();
                var response = ResponseParser.TryParse(data.AsSpan(), length);
                if (response is not null) lastResponse = response;
                connectionState = readState;
                if (drain) drainCaughtData = true;

                if (connectionState == ConnectionState.Open)
                {
                    await Task.Delay(50);
                    connectionState = client.CheckConnectionState();
                }

                stepResults.Add(new StepResult
                {
                    Label = label,
                    Executed = true,
                    Response = response,
                    ConnectionState = connectionState,
                    RawRequest = rawReq
                });
            }

            var verdict = seq.Validator(stepResults);
            var behavioralNote = seq.BehavioralAnalyzer?.Invoke(stepResults);

            // Build combined raw response for display
            var rawResponseParts = new List<string>();
            foreach (var sr in stepResults)
            {
                if (!sr.Executed)
                    rawResponseParts.Add($"── {sr.Label} ──\n[Not executed — connection closed]");
                else if (sr.Response is not null)
                    rawResponseParts.Add($"── {sr.Label} ──\n{sr.Response.RawResponse}");
                else
                    rawResponseParts.Add($"── {sr.Label} ──\n[No response]");
            }

            // Synthetic response with combined raw output for the UI
            HttpResponse? resultResponse = null;
            if (lastResponse is not null)
            {
                resultResponse = new HttpResponse
                {
                    StatusCode = lastResponse.StatusCode,
                    ReasonPhrase = lastResponse.ReasonPhrase,
                    HttpVersion = lastResponse.HttpVersion,
                    Headers = lastResponse.Headers,
                    Body = lastResponse.Body,
                    RawResponse = string.Join("\n\n", rawResponseParts)
                };
            }

            return new TestResult
            {
                TestCase = seq,
                Verdict = verdict,
                Response = resultResponse,
                ConnectionState = connectionState,
                BehavioralNote = behavioralNote,
                RawRequest = string.Join("\n\n", rawRequestParts),
                DrainCaughtData = drainCaughtData,
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestCase = seq,
                Verdict = TestVerdict.Error,
                ConnectionState = ConnectionState.Error,
                ErrorMessage = ex.Message,
                Duration = sw.Elapsed
            };
        }
    }
}
