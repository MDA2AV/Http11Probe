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

// ── GET/POST /cookie ──────────────────────────────────────────

sealed class CookieEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Verbs("GET", "POST");
        Routes("/cookie");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var cookie in HttpContext.Request.Cookies)
            sb.AppendLine($"{cookie.Key}={cookie.Value}");
        await HttpContext.Response.WriteAsync(sb.ToString(), ct);
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
