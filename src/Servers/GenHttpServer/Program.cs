using GenHTTP.Api.Protocol;

using GenHTTP.Engine.Internal;

using GenHTTP.Modules.Functional;
using GenHTTP.Modules.Practices;

var port = (args.Length > 0 && ushort.TryParse(args[0], out var p)) ? p : (ushort)8080;

var app = Inline.Create()
                .Get("/cookie", (IRequest request) => ParseCookies(request))
                .Post("/cookie", (IRequest request) => ParseCookies(request))
                .Get("/echo", (IRequest request) => Echo(request))
                .Post("/echo", (IRequest request) => Echo(request))
                .Post((Stream body) => RequestContent(body))
                .Any(() => StringContent());

return await Host.Create()
                 .Handler(app)
                 .Defaults()
                 .Port(port)
                 .RunAsync();

static string Echo(IRequest request)
{
    var headers = new System.Text.StringBuilder();

    foreach (var h in request.Headers)
    {
        headers.AppendLine($"{h.Key}: {h.Value}");
    }

    return headers.ToString();
}

static string ParseCookies(IRequest request)
{
    var sb = new System.Text.StringBuilder();
    if (request.Headers.TryGetValue("Cookie", out var cookieHeader))
    {
        foreach (var pair in cookieHeader.Split(';'))
        {
            var trimmed = pair.TrimStart();
            var eqIdx = trimmed.IndexOf('=');
            if (eqIdx > 0)
                sb.AppendLine($"{trimmed[..eqIdx]}={trimmed[(eqIdx + 1)..]}");
        }
    }
    return sb.ToString();
}

static string StringContent() => "OK";

static Stream RequestContent(Stream body) => body;
