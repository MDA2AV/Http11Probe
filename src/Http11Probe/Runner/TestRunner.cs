using System.Diagnostics;
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

    public async Task<TestRunReport> RunAsync(IEnumerable<TestCase> testCases, Action<TestResult>? onResult = null)
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

            var result = await RunSingleAsync(testCase, context);
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
}
