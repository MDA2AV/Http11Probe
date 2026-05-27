using System.Text;

using GenHTTP.Api.Protocol;

using GenHTTP.Engine.Internal;

using GenHTTP.Modules.Functional;
using GenHTTP.Modules.Practices;

var port = (args.Length > 0 && ushort.TryParse(args[0], out var p)) ? p : (ushort)8080;

var rootMethods = new  HashSet<RequestMethod> { RequestMethod.Get, RequestMethod.Head, RequestMethod.Options };

var app = Inline.Create()
                .Get("/cookie", (IRequest request) => ParseCookies(request))
                .Post("/cookie", (IRequest request) => ParseCookies(request))
                .Get("/echo", (IRequest request) => Echo(request))
                .Post("/echo", (IRequest request) => Echo(request))
                .Post((Stream body) => RequestContent(body))
                .On(() => StringContent(), rootMethods);

return await Host.Create()
                 .Handler(app)
                 .Defaults(rangeSupport: true)
                 .Port(port)
                 .RunAsync();

static string Echo(IRequest request)
{
    var headers = new StringBuilder();

    var source = request.Header.Headers;

    for (var i = 0; i < source.Count; i++)
    {
        var header = source[i];

        var key = Encoding.ASCII.GetString(header.Key.Span);
        var value = Encoding.ASCII.GetString(header.Value.Span);

        headers.AppendLine($"{key}: {value}");
    }

    return headers.ToString();
}

static string ParseCookies(IRequest request)
{
    var sb = new StringBuilder();

    var cookieHeader = request.Header.Headers.GetEntry("Cookie");

    if (cookieHeader == null)
    {
        return string.Empty;
    }

    var remaining = cookieHeader.AsSpan();

    while (!remaining.IsEmpty)
    {
        var delimiterIndex = remaining.IndexOf("; ");

        var segment = delimiterIndex >= 0
            ? remaining[..delimiterIndex]
            : remaining;

        var equalsIndex = segment.IndexOf('=');

        if (equalsIndex > 0)
        {
            var key = segment[..equalsIndex].Trim();
            var value = segment[(equalsIndex + 1)..].Trim();

            sb.AppendLine($"{key}: {value}");
        }

        remaining = delimiterIndex >= 0 ? remaining[(delimiterIndex + 2)..] : ReadOnlySpan<char>.Empty;
    }

    return sb.ToString();
}

static string StringContent() => "OK";

static Stream RequestContent(Stream body) => body;
