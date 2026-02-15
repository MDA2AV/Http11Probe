namespace Http11Probe.TestCases;

public sealed class TestContext
{
    public required string Host { get; init; }
    
    public required int Port { get; init; }

    
    public string HostHeader => Port == 80 ? Host : $"{Host}:{Port}";
}
