var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5099");

var app = builder.Build();

app.MapGet("/", () => "Hello from ASP.NET Minimal API");

app.Run();
