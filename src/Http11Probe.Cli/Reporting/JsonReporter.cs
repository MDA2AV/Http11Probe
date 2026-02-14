using System.Text.Json;
using System.Text.Json.Serialization;
using Http11Probe.Runner;
using Http11Probe.TestCases;

namespace Http11Probe.Cli.Reporting;

public static class JsonReporter
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Generate(TestRunReport report)
    {
        var output = new
        {
            summary = new
            {
                total = report.Results.Count,
                scored = report.ScoredCount,
                passed = report.PassCount,
                failed = report.FailCount,
                warnings = report.WarnCount,
                errors = report.ErrorCount,
                skipped = report.SkipCount,
                durationMs = report.TotalDuration.TotalMilliseconds
            },
            results = report.Results.Select(r => new
            {
                id = r.TestCase.Id,
                description = r.TestCase.Description,
                category = r.TestCase.Category.ToString(),
                rfcReference = r.TestCase.RfcReference,
                scored = r.TestCase.Scored,
                rfcLevel = r.TestCase.RfcLevel.ToString(),
                expected = r.TestCase.Expected.GetDescription(),
                verdict = r.Verdict.ToString(),
                statusCode = r.Response?.StatusCode,
                connectionState = r.ConnectionState.ToString(),
                error = r.ErrorMessage,
                durationMs = r.Duration.TotalMilliseconds,
                rawRequest = r.RawRequest,
                rawResponse = r.Response?.RawResponse,
                behavioralNote = r.BehavioralNote,
                doubleFlush = r.DrainCaughtData ? true : (bool?)null
            })
        };

        return JsonSerializer.Serialize(output, s_options);
    }
}
