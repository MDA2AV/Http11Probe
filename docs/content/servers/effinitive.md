---
title: "Effinitive"
description: "Effinitive (C#) tested against RFC 9110/9112 for HTTP/1.1 compliance, request smuggling resistance, and malformed input handling."
toc: false
breadcrumbs: false
---

**Language:** C# · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/EffinitiveServer)

## Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/EffinitiveServer/ src/Servers/EffinitiveServer/
RUN dotnet restore src/Servers/EffinitiveServer/EffinitiveServer.csproj
RUN dotnet publish src/Servers/EffinitiveServer/EffinitiveServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "EffinitiveServer.dll", "8080"]
```

## Source — `Program.cs`

```csharp
using System.Text;
using EffinitiveFramework.Core;
using EffinitiveFramework.Core.Http;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 8080;

var app = EffinitiveApp
    .Create()
    .UsePort(port)
    .MapEndpoints()
    .Build();

Console.WriteLine($"Effinitive listening on http://localhost:{port}");
await app.RunAsync();

namespace EffinitiveServer.Endpoints
{
    // ── GET / ──────────────────────────────────────────────────────

    sealed class GetRoot : NoRequestEndpointBase<string>
    {
        protected override string Method => "GET";
        protected override string Route => "/";
        protected override string ContentType => Helpers.TextPlain;

        public override ValueTask<string> HandleAsync(CancellationToken ct = default)
            => ValueTask.FromResult("OK");
    }

    // ── POST / ─────────────────────────────────────────────────────

    sealed class PostRoot : NoRequestEndpointBase<string>
    {
        protected override string Method => "POST";
        protected override string Route => "/";
        protected override string ContentType => Helpers.TextPlain;

        public override ValueTask<string> HandleAsync(CancellationToken ct = default)
        {
            var body = HttpContext?.Body;
            return ValueTask.FromResult(body is { Length: > 0 } ? Encoding.UTF8.GetString(body.Value.Span) : "");
        }
    }

    // ── GET/POST /echo ────────────────────────────────────────────

    sealed class EchoGet : NoRequestEndpointBase<string>
    {
        protected override string Method => "GET";
        protected override string Route => "/echo";
        protected override string ContentType => Helpers.TextPlain;

        public override ValueTask<string> HandleAsync(CancellationToken ct = default)
            => ValueTask.FromResult(Helpers.EchoHeaders(HttpContext));
    }

    sealed class EchoPost : NoRequestEndpointBase<string>
    {
        protected override string Method => "POST";
        protected override string Route => "/echo";
        protected override string ContentType => Helpers.TextPlain;

        public override ValueTask<string> HandleAsync(CancellationToken ct = default)
            => ValueTask.FromResult(Helpers.EchoHeaders(HttpContext));
    }

    // ── GET/POST /cookie ──────────────────────────────────────────

    sealed class CookieGet : NoRequestEndpointBase<string>
    {
        protected override string Method => "GET";
        protected override string Route => "/cookie";
        protected override string ContentType => Helpers.TextPlain;

        public override ValueTask<string> HandleAsync(CancellationToken ct = default)
            => ValueTask.FromResult(Helpers.ParseCookies(HttpContext));
    }

    sealed class CookiePost : NoRequestEndpointBase<string>
    {
        protected override string Method => "POST";
        protected override string Route => "/cookie";
        protected override string ContentType => Helpers.TextPlain;

        public override ValueTask<string> HandleAsync(CancellationToken ct = default)
            => ValueTask.FromResult(Helpers.ParseCookies(HttpContext));
    }

    // ── Shared helpers ────────────────────────────────────────────

    static class Helpers
    {
        public const string TextPlain = "text/plain";

        public static string EchoHeaders(HttpRequest? ctx)
        {
            if (ctx?.Headers is null) return "";
            var sb = new StringBuilder();
            foreach (var h in ctx.Headers)
                sb.Append(h.Key).Append(": ").Append(h.Value).Append("\r\n");
            return sb.ToString();
        }

        public static string ParseCookies(HttpRequest? ctx)
        {
            if (ctx is null) return "";
            var sb = new StringBuilder();
            foreach (var c in ctx.Cookies)
                sb.Append(c.Key).Append('=').Append(c.Value).Append("\r\n");
            return sb.ToString();
        }
    }
}
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

<script src="/probe/data.js"></script>
<script src="/probe/render.js"></script>
<script>
(function() {
  if (!window.PROBE_DATA) {
    document.getElementById('server-summary').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow on <code>main</code> to generate results.</em></p>';
    return;
  }
  ProbeRender.renderServerPage('Effinitive');
})();
</script>
