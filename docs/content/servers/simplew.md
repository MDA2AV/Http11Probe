---
title: "SimpleW"
toc: true
breadcrumbs: false
---

**Language:** C# · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/SimpleWServer)

## Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/SimpleWServer/ src/Servers/SimpleWServer/
RUN dotnet restore src/Servers/SimpleWServer/SimpleWServer.csproj
RUN dotnet publish src/Servers/SimpleWServer/SimpleWServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "SimpleWServer.dll", "8080"]
```

## Source

```csharp
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
```

## Test Results

<div id="server-summary"><p><em>Loading results...</em></p></div>

### Compliance

<div id="results-compliance"></div>

### Smuggling

<div id="results-smuggling"></div>

### Malformed Input

<div id="results-malformedinput"></div>

### Caching

<div id="results-capabilities"></div>

### Cookies

<div id="results-cookies"></div>

<script src="/Http11Probe/probe/data.js"></script>
<script src="/Http11Probe/probe/render.js"></script>
<script>
(function() {
  if (!window.PROBE_DATA) {
    document.getElementById('server-summary').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow on <code>main</code> to generate results.</em></p>';
    return;
  }
  ProbeRender.renderServerPage('SimpleW');
})();
</script>
