using System.Net;
using SimpleW;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 8080;

var server = new SimpleWServer(IPAddress.Any, port);

server.MapGet("/", () => "OK");
server.MapGet("/{path}", () => "OK");

server.Start();

Console.WriteLine($"SimpleW listening on http://localhost:{port}");

var waitHandle = new ManualResetEvent(false);
Console.CancelKeyPress += (_, e) => { e.Cancel = true; waitHandle.Set(); };
waitHandle.WaitOne();
