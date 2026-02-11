var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://+:8080");

var app = builder.Build();

app.MapGet("/", () => "OK");

app.MapPost("/", () => "OK");

app.Run();
