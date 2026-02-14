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
            RfcLevel = RfcLevel.Should,
            PayloadFactory = _ =>
            {
                var rng = new Random(42);
                var garbage = new byte[256];
                rng.NextBytes(garbage);
                return garbage;
            },
            Expected = new ExpectedBehavior
            {
                Description = "400/close/timeout",
                CustomValidator = (response, state) =>
                {
                    // If server sent a response, only 400 is acceptable
                    if (response is not null)
                        return response.StatusCode == 400 ? TestVerdict.Pass : TestVerdict.Fail;
                    // No response: close or timeout is acceptable
                    if (state is ConnectionState.TimedOut or ConnectionState.ClosedByServer)
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
            RfcLevel = RfcLevel.Should,
            PayloadFactory = ctx =>
            {
                var longPath = "/" + new string('A', 100_000);
                return MakeRequest($"GET {longPath} HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n");
            },
            Expected = new ExpectedBehavior
            {
                Description = "400/414/431 or close",
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
            RfcLevel = RfcLevel.Should,
            PayloadFactory = ctx =>
            {
                var longValue = new string('B', 100_000);
                return MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nX-Big: {longValue}\r\n\r\n");
            },
            Expected = new ExpectedBehavior
            {
                Description = "400/431 or close",
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
            RfcLevel = RfcLevel.NotApplicable,
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
                Description = "400/431 or close",
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
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nX-Test: value"),
            Expected = new ExpectedBehavior
            {
                Description = "400/close/timeout",
                CustomValidator = (response, state) =>
                {
                    // If server sent a response, only 400 is acceptable
                    if (response is not null)
                        return response.StatusCode == 400 ? TestVerdict.Pass : TestVerdict.Fail;
                    // No response: close or timeout is acceptable
                    if (state is ConnectionState.TimedOut or ConnectionState.ClosedByServer)
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
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = _ => [],
            Expected = new ExpectedBehavior
            {
                Description = "400/close/timeout",
                CustomValidator = (response, state) =>
                {
                    // If server sent a response, only 400 is acceptable
                    if (response is not null)
                        return response.StatusCode == 400 ? TestVerdict.Pass : TestVerdict.Fail;
                    // No response: close or timeout is acceptable
                    if (state is ConnectionState.TimedOut or ConnectionState.ClosedByServer)
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
            RfcLevel = RfcLevel.Should,
            PayloadFactory = ctx =>
            {
                var longName = new string('A', 100_000);
                return MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n{longName}: val\r\n\r\n");
            },
            Expected = new ExpectedBehavior
            {
                Description = "400/431 or close",
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
            RfcLevel = RfcLevel.Should,
            PayloadFactory = ctx =>
            {
                var longMethod = new string('A', 100_000);
                return MakeRequest($"{longMethod} / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n");
            },
            Expected = new ExpectedBehavior
            {
                Description = "400 or close",
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
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = _ => MakeRequest("   \r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400/close/timeout",
                CustomValidator = (response, state) =>
                {
                    // If server sent a response, only 400 is acceptable
                    if (response is not null)
                        return response.StatusCode == 400 ? TestVerdict.Pass : TestVerdict.Fail;
                    // No response: close or timeout is acceptable
                    if (state is ConnectionState.TimedOut or ConnectionState.ClosedByServer)
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
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = _ => Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400/505/close/timeout",
                CustomValidator = (response, state) =>
                {
                    // If server sent a response, only 400 or 505 is acceptable
                    if (response is not null)
                        return response.StatusCode is 400 or 505 ? TestVerdict.Pass : TestVerdict.Fail;
                    // No response: close or timeout is acceptable
                    if (state is ConnectionState.TimedOut or ConnectionState.ClosedByServer)
                        return TestVerdict.Pass;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-CL-EMPTY",
            Description = "Empty Content-Length value must be rejected",
            Category = TestCategory.MalformedInput,
            RfcReference = "RFC 9110 §8.6",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: \r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "MAL-CL-TAB-BEFORE-VALUE",
            Description = "Content-Length with tab as OWS — valid per RFC but unusual",
            Category = TestCategory.MalformedInput,
            RfcLevel = RfcLevel.May,
            RfcReference = "RFC 9110 §5.5",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length:\t5\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                Description = "400 or 2xx",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // 2xx = server accepted tab as OWS (RFC-compliant)
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        // ── New malformed input tests ────────────────────────────────

        yield return new TestCase
        {
            Id = "MAL-URL-BACKSLASH",
            Description = "Backslash in URL path — not valid URI character, some servers normalize to /",
            Category = TestCategory.MalformedInput,
            RfcLevel = RfcLevel.Should,
            PayloadFactory = ctx => MakeRequest(
                $"GET /path\\file HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400 or 2xx/404",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    if (response.StatusCode is >= 200 and < 500)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-URL-OVERLONG-UTF8",
            Description = "Overlong UTF-8 encoding of / (0xC0 0xAF) in URL must be rejected",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx =>
            {
                var before = Encoding.ASCII.GetBytes("GET /");
                byte[] overlong = [0xC0, 0xAF]; // Overlong UTF-8 for 0x2F (/)
                var after = Encoding.ASCII.GetBytes($" HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n");
                var payload = new byte[before.Length + overlong.Length + after.Length];
                before.CopyTo(payload, 0);
                overlong.CopyTo(payload, before.Length);
                after.CopyTo(payload, before.Length + overlong.Length);
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
            Id = "MAL-URL-PERCENT-NULL",
            Description = "Percent-encoded NUL byte (%00) in URL — security risk from null byte injection",
            Category = TestCategory.MalformedInput,
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = ctx => MakeRequest(
                $"GET /path%00.html HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400 or 2xx/404",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    if (response.StatusCode is >= 200 and < 500)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-URL-PERCENT-CRLF",
            Description = "Percent-encoded CRLF (%0d%0a) in URL — header injection if server decodes during parsing",
            Category = TestCategory.MalformedInput,
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = ctx => MakeRequest(
                $"GET /path%0d%0aX-Injected:%20true HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400 or 2xx/404",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    if (response.StatusCode is >= 200 and < 500)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-CHUNK-EXT-64K",
            Description = "64KB chunk extension — tests extension length limits (CVE-2023-39326 class)",
            Category = TestCategory.MalformedInput,
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = ctx =>
            {
                var longExt = new string('a', 65_536);
                return MakeRequest(
                    $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5;ext={longExt}\r\nhello\r\n0\r\n\r\n");
            },
            Expected = new ExpectedBehavior
            {
                Description = "400 or 2xx",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode is 400 or 431)
                        return TestVerdict.Pass;
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-RANGE-OVERLAPPING",
            Description = "1000 overlapping Range values — resource exhaustion vector (CVE-2011-3192 class)",
            Category = TestCategory.MalformedInput,
            RfcLevel = RfcLevel.NotApplicable,
            Scored = false,
            PayloadFactory = ctx =>
            {
                var ranges = string.Join(",", Enumerable.Range(0, 1000).Select(_ => "0-"));
                return MakeRequest(
                    $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nRange: bytes={ranges}\r\n\r\n");
            },
            Expected = new ExpectedBehavior
            {
                Description = "200/206/400/416",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    // Any of these is acceptable
                    if (response.StatusCode is 200 or 206 or 400 or 416 or 431)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "MAL-POST-CL-HUGE-NO-BODY",
            Description = "POST with Content-Length: 999999999 but no body — tests timeout vs memory allocation",
            Category = TestCategory.MalformedInput,
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 999999999\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400/close/timeout",
                CustomValidator = (response, state) =>
                {
                    // If server sent a response, only 400 is acceptable
                    if (response is not null)
                        return response.StatusCode == 400 ? TestVerdict.Pass : TestVerdict.Fail;
                    // No response: close or timeout means server correctly waited
                    if (state is ConnectionState.TimedOut or ConnectionState.ClosedByServer)
                        return TestVerdict.Pass;
                    return TestVerdict.Fail;
                }
            }
        };
    }

    private static byte[] MakeRequest(string request) => Encoding.ASCII.GetBytes(request);
}
