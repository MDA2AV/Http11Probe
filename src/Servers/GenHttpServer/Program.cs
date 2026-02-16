using GenHTTP.Api.Protocol;

using GenHTTP.Engine.Internal;

using GenHTTP.Modules.Functional;
using GenHTTP.Modules.Practices;

var port = (args.Length > 0 && ushort.TryParse(args[0], out var p)) ? p : (ushort)8080;

var app = Inline.Create()
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

static string StringContent() => "OK";

static Stream RequestContent(Stream body) => body;
