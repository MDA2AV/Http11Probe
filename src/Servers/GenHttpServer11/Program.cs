using System.Text;

using GenHTTP.Api.Protocol;

using GenHTTP.Engine.Internal;

using GenHTTP.Modules.Functional;
using GenHTTP.Modules.Practices;

var port = (args.Length > 0 && ushort.TryParse(args[0], out var p)) ? p : (ushort)8080;

var rootMethods = new HashSet<RequestMethod> { RequestMethod.Get, RequestMethod.Head, RequestMethod.Options };

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

        headers.AppendLine($"{header.Key}: {header.Value}");
    }

    return headers.ToString();
}

static string ParseCookies(IRequest request)
{
    var sb = new StringBuilder();

    var cookies = request.Header.Headers.GetCookies();

    for (var i = 0; i < cookies.Count; i++)
    {
        var cookie = cookies[i];

        sb.AppendLine($"{cookie.Key}: {cookie.Value}");
    }

    return sb.ToString();
}

static string StringContent() => "OK";

static Stream RequestContent(Stream body) => body;
