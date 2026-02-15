using Http11Probe.Response;

namespace Http11Probe.TestCases;

public sealed class TestCase
{
    public required string Id { get; init; }
    
    public required string Description { get; init; }
    
    public required TestCategory Category { get; init; }
    
    public string? RfcReference { get; init; }
    
    public required Func<TestContext, byte[]> PayloadFactory { get; init; }
    
    public required ExpectedBehavior Expected { get; init; }
    
    public bool Scored { get; init; } = true;
    
    public RfcLevel RfcLevel { get; init; } = RfcLevel.Must;
    
    public Func<HttpResponse?, string?>? BehavioralAnalyzer { get; init; }
}
