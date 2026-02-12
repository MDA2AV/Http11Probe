using WatsonWebserver;
using WatsonWebserver.Core;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 8080;

var settings = new WebserverSettings("*", port);
var server = new Webserver(settings, async ctx =>
{
    ctx.Response.StatusCode = 200;
    ctx.Response.ContentType = "text/plain";
    if (ctx.Request.Method == WatsonWebserver.Core.HttpMethod.POST && ctx.Request.Data != null)
    {
        using var reader = new StreamReader(ctx.Request.Data);
        var body = await reader.ReadToEndAsync();
        await ctx.Response.Send(body);
    }
    else
    {
        await ctx.Response.Send("OK");
    }
});

server.Start();

Console.WriteLine($"Watson listening on http://localhost:{port}");

var waitHandle = new ManualResetEvent(false);
Console.CancelKeyPress += (_, e) => { e.Cancel = true; waitHandle.Set(); };
waitHandle.WaitOne();

server.Stop();
