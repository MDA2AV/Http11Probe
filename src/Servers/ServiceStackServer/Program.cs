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
