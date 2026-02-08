using System.Text;
using Http11Probe.Client;

namespace Http11Probe.TestCases.Suites;

public static class MalformedInputSuite
{
    public static IEnumerable<TestCase> GetTestCases()
    {
        yield return new TestCase
        {
            Id = "MAL-BINARY-GARBAGE",
            Description = "Random binary garbage should be rejected or connection closed",
            Category = TestCategory.MalformedInput,
            PayloadFactory = _ =>
            {
                var rng = new Random(42);
                var garbage = new byte[256];
                rng.NextBytes(garbage);
                return garbage;
            },
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    // Any of these is acceptable: 400, close, or timeout
                    if (state is ConnectionState.TimedOut or ConnectionState.ClosedByServer)
                        return TestVerdict.Pass;
                    if (response is not null && response.StatusCode == 400)
                        return TestVerdict.Pass;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-LONG-URL",
            Description = "100KB URL should be rejected with 414 URI Too Long",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx =>
            {
                var longPath = "/" + new string('A', 100_000);
                return MakeRequest($"GET {longPath} HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n");
            },
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    // 414 is ideal, 400 and 431 are also acceptable
                    return response.StatusCode is 400 or 414 or 431
                        ? TestVerdict.Pass
                        : TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-LONG-HEADER-VALUE",
            Description = "100KB header value should be rejected with 431",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx =>
            {
                var longValue = new string('B', 100_000);
                return MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nX-Big: {longValue}\r\n\r\n");
            },
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    return response.StatusCode is 400 or 431
                        ? TestVerdict.Pass
                        : TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-MANY-HEADERS",
            Description = "10,000 headers should be rejected with 431",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx =>
            {
                var sb = new StringBuilder();
                sb.Append($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n");
                for (var i = 0; i < 10_000; i++)
                    sb.Append($"X-H-{i}: value\r\n");
                sb.Append("\r\n");
                return Encoding.ASCII.GetBytes(sb.ToString());
            },
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    return response.StatusCode is 400 or 431
                        ? TestVerdict.Pass
                        : TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-NUL-IN-URL",
            Description = "NUL byte in URL should be rejected",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx => MakeRequest($"GET /\0test HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "MAL-CONTROL-CHARS-HEADER",
            Description = "Control characters in header value should be rejected",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx =>
            {
                // Inject BEL (0x07), BS (0x08), VT (0x0B) into header value
                var request = $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nX-Test: abc\x07\x08\x0Bdef\r\n\r\n";
                return Encoding.ASCII.GetBytes(request);
            },
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "MAL-INCOMPLETE-REQUEST",
            Description = "Partial HTTP request — request-line and headers but no final CRLF",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nX-Test: value"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    // Any of these is acceptable: timeout, close, or 400
                    if (state is ConnectionState.TimedOut or ConnectionState.ClosedByServer)
                        return TestVerdict.Pass;
                    if (response is not null && response.StatusCode == 400)
                        return TestVerdict.Pass;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-EMPTY-REQUEST",
            Description = "Zero bytes — TCP connection established without sending any data",
            Category = TestCategory.MalformedInput,
            PayloadFactory = _ => [],
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (state is ConnectionState.TimedOut or ConnectionState.ClosedByServer)
                        return TestVerdict.Pass;
                    if (response is not null && response.StatusCode == 400)
                        return TestVerdict.Pass;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-LONG-HEADER-NAME",
            Description = "100KB header name should be rejected with 400/431",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx =>
            {
                var longName = new string('A', 100_000);
                return MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n{longName}: val\r\n\r\n");
            },
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    return response.StatusCode is 400 or 431
                        ? TestVerdict.Pass
                        : TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-LONG-METHOD",
            Description = "100KB method name should be rejected",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx =>
            {
                var longMethod = new string('A', 100_000);
                return MakeRequest($"{longMethod} / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n");
            },
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    return response.StatusCode == 400
                        ? TestVerdict.Pass
                        : TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-NON-ASCII-HEADER-NAME",
            Description = "Non-ASCII bytes (UTF-8 ë) in header name must be rejected",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx =>
            {
                // Build raw bytes: can't use Encoding.ASCII for non-ASCII
                var before = Encoding.ASCII.GetBytes($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nX-T");
                byte[] utf8Bytes = [0xC3, 0xAB]; // UTF-8 ë
                var after = Encoding.ASCII.GetBytes("st: value\r\n\r\n");
                var payload = new byte[before.Length + utf8Bytes.Length + after.Length];
                before.CopyTo(payload, 0);
                utf8Bytes.CopyTo(payload, before.Length);
                after.CopyTo(payload, before.Length + utf8Bytes.Length);
                return payload;
            },
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "MAL-NON-ASCII-URL",
            Description = "Non-ASCII bytes (UTF-8 é) in URL must be rejected",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx =>
            {
                // Build raw bytes: can't use Encoding.ASCII for non-ASCII
                var before = Encoding.ASCII.GetBytes("GET /caf");
                byte[] utf8Bytes = [0xC3, 0xA9]; // UTF-8 é
                var after = Encoding.ASCII.GetBytes($" HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n");
                var payload = new byte[before.Length + utf8Bytes.Length + after.Length];
                before.CopyTo(payload, 0);
                utf8Bytes.CopyTo(payload, before.Length);
                after.CopyTo(payload, before.Length + utf8Bytes.Length);
                return payload;
            },
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "MAL-CL-OVERFLOW",
            Description = "Content-Length with integer overflow value must be rejected",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 99999999999999999999\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "MAL-WHITESPACE-ONLY-LINE",
            Description = "Whitespace-only request line should be rejected or timeout",
            Category = TestCategory.MalformedInput,
            PayloadFactory = _ => MakeRequest("   \r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (state is ConnectionState.TimedOut or ConnectionState.ClosedByServer)
                        return TestVerdict.Pass;
                    if (response is not null && response.StatusCode == 400)
                        return TestVerdict.Pass;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-NUL-IN-HEADER-VALUE",
            Description = "NUL byte in header value should be rejected",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx =>
            {
                var request = $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nX-Test: val\0ue\r\n\r\n";
                return Encoding.ASCII.GetBytes(request);
            },
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "MAL-CHUNK-SIZE-OVERFLOW",
            Description = "Chunk size with integer overflow must be rejected",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\nFFFFFFFFFFFFFFFF0\r\nhello\r\n0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "MAL-H2-PREFACE",
            Description = "HTTP/2 connection preface sent to HTTP/1.1 server must be rejected",
            Category = TestCategory.MalformedInput,
            PayloadFactory = _ => Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (state is ConnectionState.TimedOut or ConnectionState.ClosedByServer)
                        return TestVerdict.Pass;
                    if (response is not null && response.StatusCode is 400 or 505)
                        return TestVerdict.Pass;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-CHUNK-EXTENSION-LONG",
            Description = "Chunk extension with 100KB value should be rejected",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx =>
            {
                var longExt = new string('A', 100_000);
                return MakeRequest(
                    $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5;ext={longExt}\r\nhello\r\n0\r\n\r\n");
            },
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    return response.StatusCode is 400 or 431
                        ? TestVerdict.Pass
                        : TestVerdict.Fail;
                }
            }
        };
    }

    private static byte[] MakeRequest(string request) => Encoding.ASCII.GetBytes(request);
}
