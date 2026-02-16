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
