using Sisk.Core.Http;
using Sisk.Core.Routing;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 8080;

using var app = HttpServer.CreateBuilder()
    .UseListeningPort($"http://+:{port}/")
    .Build();

app.Router.SetRoute(RouteMethod.Any, Route.AnyPath, request =>
{
    if (request.Method == HttpMethod.Post && request.Body is not null)
    {
        var body = request.Body;
        return new HttpResponse(200).WithContent(body);
    }
    return new HttpResponse(200).WithContent("OK");
});

await app.StartAsync();
