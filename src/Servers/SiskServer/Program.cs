using Sisk.Core.Http;
using Sisk.Core.Routing;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 8080;

using var app = HttpServer.CreateBuilder()
    .UseListeningPort($"http://+:{port}/")
    .Build();

app.Router.SetRoute(RouteMethod.Any, Route.AnyPath, request =>
{
    if (request.Path == "/echo")
    {
        var sb = new System.Text.StringBuilder();
        foreach (var h in request.Headers)
            foreach (var val in h.Value)
                sb.AppendLine($"{h.Key}: {val}");
        return new HttpResponse(200).WithContent(sb.ToString());
    }
    if (request.Path == "/cookie")
    {
        var sb = new System.Text.StringBuilder();
        foreach (var h in request.Headers)
        {
            if (string.Equals(h.Key, "Cookie", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var rawVal in h.Value)
                {
                    foreach (var pair in rawVal.Split(';'))
                    {
                        var trimmed = pair.TrimStart();
                        var eqIdx = trimmed.IndexOf('=');
                        if (eqIdx > 0)
                            sb.AppendLine($"{trimmed[..eqIdx]}={trimmed[(eqIdx + 1)..]}");
                    }
                }
            }
        }
        return new HttpResponse(200).WithContent(sb.ToString());
    }
    if (request.Method == HttpMethod.Post && request.Body is not null)
    {
        var body = request.Body;
        return new HttpResponse(200).WithContent(body);
    }
    return new HttpResponse(200).WithContent("OK");
});

await app.StartAsync();
