namespace Http11Probe.TestCases;

public sealed class SequenceSendPart
{
    public required Func<TestContext, byte[]> PayloadFactory { get; init; }

    public TimeSpan DelayAfter { get; init; } = TimeSpan.Zero;

    // Optional label used for display in the raw request view.
    public string? Label { get; init; }
}

