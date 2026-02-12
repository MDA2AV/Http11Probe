using Nancy;
using Nancy.Hosting.Self;

var port = args.Length > 0 ? args[0] : "9006";
var uri = new Uri($"http://0.0.0.0:{port}");

var config = new HostConfiguration { RewriteLocalhost = false };

using var host = new NancyHost(config, uri);
host.Start();

Console.WriteLine($"Nancy listening on {uri}");

var waitHandle = new ManualResetEvent(false);
Console.CancelKeyPress += (_, e) => { e.Cancel = true; waitHandle.Set(); };
waitHandle.WaitOne();

public class HomeModule : NancyModule
{
    public HomeModule()
    {
        Get("/{path*}", _ => "OK");
        Get("/", _ => "OK");
        Post("/{path*}", _ => "OK");
        Post("/", _ => "OK");
    }
}
