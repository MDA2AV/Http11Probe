---
title: "ServiceStack"
toc: true
breadcrumbs: false
---

**Language:** C# · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/ServiceStackServer)

## Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/ServiceStackServer/ src/Servers/ServiceStackServer/
RUN dotnet restore src/Servers/ServiceStackServer/ServiceStackServer.csproj
RUN dotnet publish src/Servers/ServiceStackServer/ServiceStackServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ServiceStackServer.dll"]
```

## Source

```csharp
using ServiceStack;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseServiceStack(new AppHost());
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
app.MapFallback(async (HttpContext ctx) =>
{
    if (ctx.Request.Method == "POST")
    {
        using var reader = new StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        return Results.Text(body);
    }
    return Results.Ok("OK");
});
app.Run("http://0.0.0.0:8080");

class AppHost : AppHostBase
{
    public AppHost() : base("Probe", typeof(AppHost).Assembly) { }
    public override void Configure() { }
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

<script src="/Http11Probe/probe/data.js"></script>
<script src="/Http11Probe/probe/render.js"></script>
<script>
(function() {
  if (!window.PROBE_DATA) {
    document.getElementById('server-summary').innerHTML = '<p><em>No probe data available yet. Run the Probe workflow on <code>main</code> to generate results.</em></p>';
    return;
  }
  ProbeRender.renderServerPage('ServiceStack');
})();
</script>
