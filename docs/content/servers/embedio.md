---
title: "EmbedIO"
toc: true
breadcrumbs: false
---

**Language:** C# · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/EmbedIOServer)

## Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/EmbedIOServer/ src/Servers/EmbedIOServer/
RUN dotnet restore src/Servers/EmbedIOServer/EmbedIOServer.csproj
RUN dotnet publish src/Servers/EmbedIOServer/EmbedIOServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "EmbedIOServer.dll", "8080"]
```

## Source

```csharp
using EmbedIO;
using EmbedIO.Actions;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 8080;
var url = $"http://*:{port}/";

using var server = new WebServer(o => o
    .WithUrlPrefix(url)
    .WithMode(HttpListenerMode.EmbedIO))
    .WithModule(new ActionModule("/cookie", HttpVerbs.Any, async ctx =>
    {
        var sb = new System.Text.StringBuilder();
        foreach (System.Net.Cookie cookie in ctx.Request.Cookies)
            sb.AppendLine($"{cookie.Name}={cookie.Value}");
        await ctx.SendStringAsync(sb.ToString(), "text/plain", System.Text.Encoding.UTF8);
    }))
    .WithModule(new ActionModule("/echo", HttpVerbs.Any, async ctx =>
    {
        var sb = new System.Text.StringBuilder();
        foreach (var key in ctx.Request.Headers.AllKeys)
            foreach (var val in ctx.Request.Headers.GetValues(key)!)
                sb.AppendLine($"{key}: {val}");
        await ctx.SendStringAsync(sb.ToString(), "text/plain", System.Text.Encoding.UTF8);
    }))
    .WithModule(new ActionModule("/", HttpVerbs.Any, async ctx =>
    {
        ctx.Response.ContentType = "text/plain";
        if (ctx.Request.HttpVerb == HttpVerbs.Post)
        {
            using var reader = new System.IO.StreamReader(ctx.Request.InputStream);
            var body = await reader.ReadToEndAsync();
            await ctx.SendStringAsync(body, "text/plain", System.Text.Encoding.UTF8);
        }
        else
        {
            await ctx.SendStringAsync("OK", "text/plain", System.Text.Encoding.UTF8);
        }
    }));

Console.WriteLine($"EmbedIO listening on http://localhost:{port}");
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
  ProbeRender.renderServerPage('EmbedIO');
})();
</script>
