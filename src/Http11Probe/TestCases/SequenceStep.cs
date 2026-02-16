namespace Http11Probe.TestCases;

public sealed class SequenceStep
{
    // One-shot send (existing behavior)
    public Func<TestContext, byte[]>? PayloadFactory { get; init; }

    // Multi-part send with optional delays between parts (used for pause-based desync / partial sends).
    public Func<TestContext, IReadOnlyList<SequenceSendPart>>? SendPartsFactory { get; init; }
    public string? Label { get; init; }
}
