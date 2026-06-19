using System.Buffers;
using System.Text;
using EffinitiveFramework.Core;
using EffinitiveFramework.Core.Http;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 8080;

var app = EffinitiveApp
    .Create()
    .UsePort(port)
    .MapEndpoints()
    .Build();

Console.WriteLine($"Effinitive listening on http://localhost:{port}");
await app.RunAsync();

namespace EffinitiveServer.Endpoints
{
    // ── GET / ──────────────────────────────────────────────────────

    sealed class GetRoot : NoRequestEndpointBase<string>
    {
        protected override string Method => "GET";
        protected override string Route => "/";
        protected override string ContentType => Helpers.TextPlain;

        public override ValueTask<string> HandleAsync(CancellationToken ct = default)
            => ValueTask.FromResult("OK");
    }

    // ── POST / ─────────────────────────────────────────────────────

    sealed class PostRoot : NoRequestEndpointBase<string>
    {
        protected override string Method => "POST";
        protected override string Route => "/";
        protected override string ContentType => Helpers.TextPlain;

        public override async ValueTask<string> HandleAsync(CancellationToken ct = default)
        {
            if (HttpContext?.BodyDeferred == true && HttpContext.BodyStream != null)
            {
                using var ms = new MemoryStream();
                var buf = ArrayPool<byte>.Shared.Rent(4096);
                try
                {
                    int r;
                    while ((r = await HttpContext.BodyStream.ReadAsync(buf.AsMemory(), ct)) > 0)
                        ms.Write(buf, 0, r);
                }
                finally { ArrayPool<byte>.Shared.Return(buf); }
                return ms.Length > 0 ? Encoding.UTF8.GetString(ms.ToArray()) : "";
            }
            var body = HttpContext?.Body;
            return body is { Length: > 0 } ? Encoding.UTF8.GetString(body.Value.Span) : "";
        }
    }

    // ── GET/POST /echo ────────────────────────────────────────────

    sealed class EchoGet : NoRequestEndpointBase<string>
    {
        protected override string Method => "GET";
        protected override string Route => "/echo";
        protected override string ContentType => Helpers.TextPlain;

        public override ValueTask<string> HandleAsync(CancellationToken ct = default)
            => ValueTask.FromResult(Helpers.EchoHeaders(HttpContext));
    }

    sealed class EchoPost : NoRequestEndpointBase<string>
    {
        protected override string Method => "POST";
        protected override string Route => "/echo";
        protected override string ContentType => Helpers.TextPlain;

        public override ValueTask<string> HandleAsync(CancellationToken ct = default)
            => ValueTask.FromResult(Helpers.EchoHeaders(HttpContext));
    }

    // ── GET/POST /cookie ──────────────────────────────────────────

    sealed class CookieGet : NoRequestEndpointBase<string>
    {
        protected override string Method => "GET";
        protected override string Route => "/cookie";
        protected override string ContentType => Helpers.TextPlain;

        public override ValueTask<string> HandleAsync(CancellationToken ct = default)
            => ValueTask.FromResult(Helpers.ParseCookies(HttpContext));
    }

    sealed class CookiePost : NoRequestEndpointBase<string>
    {
        protected override string Method => "POST";
        protected override string Route => "/cookie";
        protected override string ContentType => Helpers.TextPlain;

        public override ValueTask<string> HandleAsync(CancellationToken ct = default)
            => ValueTask.FromResult(Helpers.ParseCookies(HttpContext));
    }

    // ── Shared helpers ────────────────────────────────────────────

    static class Helpers
    {
        public const string TextPlain = "text/plain";

        public static string EchoHeaders(HttpRequest? ctx)
        {
            if (ctx?.Headers is null) return "";
            var sb = new StringBuilder();
            foreach (var h in ctx.Headers)
                sb.Append(h.Key).Append(": ").Append(h.Value).Append("\r\n");
            return sb.ToString();
        }

        public static string ParseCookies(HttpRequest? ctx)
        {
            if (ctx is null) return "";
            var sb = new StringBuilder();
            foreach (var c in ctx.Cookies)
                sb.Append(c.Key).Append('=').Append(c.Value).Append("\r\n");
            return sb.ToString();
        }
    }
}
