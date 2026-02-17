using System.Net;
using SimpleW;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 8080;

var server = new SimpleWServer(IPAddress.Any, port);


server.MapGet("/cookie", (HttpSession session) => ParseCookies(session));
server.MapPost("/cookie", (HttpSession session) => ParseCookies(session));
server.MapGet("/echo", (HttpSession session) =>
{
    var sb = new System.Text.StringBuilder();
    foreach (var h in session.Request.Headers.EnumerateAll())
        sb.AppendLine($"{h.Key}: {h.Value}");
    return sb.ToString();
});
server.MapPost("/echo", (HttpSession session) =>
{
    var sb = new System.Text.StringBuilder();
    foreach (var h in session.Request.Headers.EnumerateAll())
        sb.AppendLine($"{h.Key}: {h.Value}");
    return sb.ToString();
});
server.MapGet("/", () => "OK");
server.MapGet("/{path}", () => "OK");
server.MapPost("/", (HttpSession session) => session.Request.BodyString);
server.MapPost("/{path}", (HttpSession session) => session.Request.BodyString);

static string ParseCookies(HttpSession session)
{
    var sb = new System.Text.StringBuilder();
    foreach (var h in session.Request.Headers.EnumerateAll())
    {
        if (string.Equals(h.Key, "Cookie", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var pair in h.Value.Split(';'))
            {
                var trimmed = pair.TrimStart();
                var eqIdx = trimmed.IndexOf('=');
                if (eqIdx > 0)
                    sb.AppendLine($"{trimmed[..eqIdx]}={trimmed[(eqIdx + 1)..]}");
            }
        }
    }
    return sb.ToString();
}

Console.WriteLine($"SimpleW listening on http://localhost:{port}");

await server.RunAsync();
