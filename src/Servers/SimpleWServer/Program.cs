using System.Net;
using SimpleW;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 8080;

var server = new SimpleWServer(IPAddress.Any, port);


server.MapGet("/", () => "OK");
server.MapGet("/{path}", () => "OK");
server.MapPost("/", (HttpSession session) => session.Request.BodyString);
server.MapPost("/{path}", (HttpSession session) => session.Request.BodyString);

Console.WriteLine($"SimpleW listening on http://localhost:{port}");

await server.RunAsync();
