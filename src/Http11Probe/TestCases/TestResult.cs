using Http11Probe.Client;
using Http11Probe.Response;

namespace Http11Probe.TestCases;

public sealed class TestResult
{
    public required TestCase TestCase { get; init; }
    public required TestVerdict Verdict { get; init; }
    public HttpResponse? Response { get; init; }
    public HttpResponse? FollowUpResponse { get; init; }
    public ConnectionState ConnectionState { get; init; }
    public string? ErrorMessage { get; init; }
    public string? BehavioralNote { get; init; }
    public string? RawRequest { get; init; }
    public TimeSpan Duration { get; init; }
}
