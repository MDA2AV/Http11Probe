using Http11Probe.TestCases;

namespace Http11Probe.Runner;

public sealed class TestRunReport
{
    public required IReadOnlyList<TestResult> Results { get; init; }
    
    public required TimeSpan TotalDuration { get; init; }
    

    public int PassCount => Results.Count(r => r.TestCase.Scored && r.Verdict == TestVerdict.Pass);
    
    public int FailCount => Results.Count(r => r.TestCase.Scored && r.Verdict == TestVerdict.Fail);
    
    public int WarnCount => Results.Count(r => r.TestCase.Scored && r.Verdict == TestVerdict.Warn);
    
    public int ScoredCount => PassCount + FailCount + WarnCount;
    
    public int SkipCount => Results.Count(r => r.Verdict == TestVerdict.Skip);
    
    public int ErrorCount => Results.Count(r => r.Verdict == TestVerdict.Error);
    
    public int UnscoredCount => Results.Count(r => !r.TestCase.Scored && r.Verdict != TestVerdict.Skip);
}
