var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://+:8080");

var app = builder.Build();

app.MapGet("/", () => "OK");

app.MapPost("/", async (HttpContext ctx) =>
{
    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();
    return Results.Text(body);
});

app.Run();
