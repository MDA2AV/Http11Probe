using ServiceStack;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseServiceStack(new AppHost());
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
