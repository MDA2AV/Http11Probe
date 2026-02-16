---
title: "Sisk"
toc: true
breadcrumbs: false
---

**Language:** C# · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/SiskServer)

## Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/SiskServer/ src/Servers/SiskServer/
RUN dotnet restore src/Servers/SiskServer/SiskServer.csproj
RUN dotnet publish src/Servers/SiskServer/SiskServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "SiskServer.dll", "8080"]
```

## Source

```csharp
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
  ProbeRender.renderServerPage('Sisk');
})();
</script>
