---
title: "FastEndpoints"
toc: false
breadcrumbs: false
---

**Language:** C# · [View source on GitHub](https://github.com/MDA2AV/Http11Probe/tree/main/src/Servers/FastEndpointsServer)

## Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/Servers/FastEndpointsServer/ src/Servers/FastEndpointsServer/
RUN dotnet restore src/Servers/FastEndpointsServer/FastEndpointsServer.csproj
RUN dotnet publish src/Servers/FastEndpointsServer/FastEndpointsServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "FastEndpointsServer.dll"]
```

## Source — `Program.cs`

```csharp
using FastEndpoints;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://+:8080");
builder.Services.AddFastEndpoints(o => o.Assemblies = [typeof(GetRoot).Assembly]);

var app = builder.Build();

app.UseFastEndpoints();

app.Run();

// ── GET / ──────────────────────────────────────────────────────

sealed class GetRoot : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await HttpContext.Response.WriteAsync("OK", ct);
    }
}

// ── HEAD / ─────────────────────────────────────────────────────

sealed class HeadRoot : EndpointWithoutRequest
{
    public override void Configure()
    {
        Verbs("HEAD");
        Routes("/");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        HttpContext.Response.StatusCode = 200;
        await HttpContext.Response.WriteAsync("", ct);
    }
}

// ── POST / ─────────────────────────────────────────────────────

sealed class PostRoot : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        using var reader = new StreamReader(HttpContext.Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        await HttpContext.Response.WriteAsync(body, ct);
    }
}

// ── OPTIONS / ──────────────────────────────────────────────────

sealed class OptionsRoot : EndpointWithoutRequest
{
    public override void Configure()
    {
        Verbs("OPTIONS");
        Routes("/");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        HttpContext.Response.Headers["Allow"] = "GET, HEAD, POST, OPTIONS";
        HttpContext.Response.StatusCode = 200;
        await HttpContext.Response.WriteAsync("", ct);
    }
}

// ── POST /echo ─────────────────────────────────────────────────

sealed class PostEcho : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/echo");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var h in HttpContext.Request.Headers)
            foreach (var v in h.Value)
                sb.AppendLine($"{h.Key}: {v}");
        await HttpContext.Response.WriteAsync(sb.ToString(), ct);
    }
}
```

## Source — `FastEndpointsServer.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="FastEndpoints" Version="7.2.0" />
    </ItemGroup>

</Project>
```
