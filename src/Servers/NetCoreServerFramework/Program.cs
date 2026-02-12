using System.Net;
using System.Net.Sockets;
using NetCoreServer;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 8080;

var server = new OkHttpServer(IPAddress.Any, port);
server.Start();

Console.WriteLine($"NetCoreServer listening on http://localhost:{port}");

var waitHandle = new ManualResetEvent(false);
Console.CancelKeyPress += (_, e) => { e.Cancel = true; waitHandle.Set(); };
waitHandle.WaitOne();

server.Stop();

class OkHttpSession : HttpSession
{
    public OkHttpSession(NetCoreServer.HttpServer server) : base(server) { }

    protected override void OnReceivedRequest(HttpRequest request)
    {
        if (request.Method == "POST" && request.Body.Length > 0)
            SendResponseAsync(Response.MakeOkResponse(200).SetBody(request.Body));
        else
            SendResponseAsync(Response.MakeOkResponse(200).SetBody("OK"));
    }

    protected override void OnReceivedRequestError(HttpRequest request, string error)
    {
        SendResponseAsync(Response.MakeErrorResponse(400));
    }

    protected override void OnError(SocketError error) { }
}

class OkHttpServer : NetCoreServer.HttpServer
{
    public OkHttpServer(IPAddress address, int port) : base(address, port) { }

    protected override TcpSession CreateSession() => new OkHttpSession(this);

    protected override void OnError(SocketError error) { }
}
