namespace Http11Probe.Response;

public sealed class HttpResponse
{
    public required int StatusCode { get; init; }
    
    public required string ReasonPhrase { get; init; }
    
    public required string HttpVersion { get; init; }
    
    public required IReadOnlyDictionary<string, string> Headers { get; init; }
    
    public bool IsEmpty { get; init; }
    
    public string? RawResponse { get; init; }
    
    public string? Body { get; init; }
}
