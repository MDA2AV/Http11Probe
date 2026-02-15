using Http11Probe.TestCases;

namespace Http11Probe.Runner;

public sealed class TestRunOptions
{
    public required string Host { get; init; }
    
    public required int Port { get; init; }
    
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(5);
    
    public TimeSpan ReadTimeout { get; init; } = TimeSpan.FromSeconds(5);
    
    public TestCategory? CategoryFilter { get; init; }
    
    public HashSet<string>? TestIdFilter { get; init; }
}
