using ServiceStack;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseServiceStack(new AppHost());
app.MapFallback(() => Results.Ok("OK"));
app.Run("http://0.0.0.0:8080");

class AppHost : AppHostBase
{
    public AppHost() : base("Probe", typeof(AppHost).Assembly) { }
    public override void Configure() { }
}
