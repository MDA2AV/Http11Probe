---
title: "ASP.NET Minimal"
toc: true
breadcrumbs: false
---

**Language:** C# · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/AspNetMinimal)

## Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/AspNetMinimal/ src/Servers/AspNetMinimal/
RUN dotnet restore src/Servers/AspNetMinimal/AspNetMinimal.csproj
RUN dotnet publish src/Servers/AspNetMinimal/AspNetMinimal.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "AspNetMinimal.dll"]
```

## Source

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://+:8080");

var app = builder.Build();

app.MapGet("/", () => "OK");

app.MapMethods("/", ["HEAD"], () => Results.Ok());

app.MapMethods("/", ["OPTIONS"], (HttpContext ctx) =>
{
    ctx.Response.Headers["Allow"] = "GET, HEAD, POST, OPTIONS";
    return Results.Ok();
});

app.MapPost("/", async (HttpContext ctx) =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();
    return Results.Text(body);
});

app.Map("/echo", (HttpContext ctx) =>
{
    var sb = new System.Text.StringBuilder();
    foreach (var h in ctx.Request.Headers)
        foreach (var v in h.Value)
            sb.AppendLine($"{h.Key}: {v}");
    return Results.Text(sb.ToString());
});

app.Map("/cookie", (HttpContext ctx) =>
{
    var sb = new System.Text.StringBuilder();
    foreach (var cookie in ctx.Request.Cookies)
        sb.AppendLine($"{cookie.Key}={cookie.Value}");
    return Results.Text(sb.ToString());
});

app.Run();
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
  ProbeRender.renderServerPage('Kestrel');
})();
</script>
