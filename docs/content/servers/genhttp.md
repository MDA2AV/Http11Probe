---
title: "GenHTTP"
toc: true
breadcrumbs: false
---

**Language:** C# · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/GenHttpServer)

## Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/GenHttpServer/ src/Servers/GenHttpServer/
RUN dotnet restore src/Servers/GenHttpServer/GenHttpServer.csproj
RUN dotnet publish src/Servers/GenHttpServer/GenHttpServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "GenHttpServer.dll", "8080"]
```

## Source

```csharp
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
  ProbeRender.renderServerPage('GenHTTP');
})();
</script>
