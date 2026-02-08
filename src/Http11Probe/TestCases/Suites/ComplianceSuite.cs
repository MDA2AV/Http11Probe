using System.Text;
using Http11Probe.Client;

namespace Http11Probe.TestCases.Suites;

public static class ComplianceSuite
{
    public static IEnumerable<TestCase> GetTestCases()
    {
        yield return new TestCase
        {
            Id = "COMP-BASELINE",
            Description = "Valid GET request — confirms server is reachable",
            Category = TestCategory.Compliance,
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range2xx
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-2.2-BARE-LF-REQUEST-LINE",
            Description = "Bare LF in request line must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §2.2",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-2.2-BARE-LF-HEADER",
            Description = "Bare LF in header must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §2.2",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\nX-Test: value\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-5.1-OBS-FOLD",
            Description = "Obs-fold (line folding) in headers should be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §5.1",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nX-Test: value\r\n continued\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400)
            }
        };

        yield return new TestCase
        {
            Id = "RFC9110-5.6.2-SP-BEFORE-COLON",
            Description = "Whitespace between header name and colon must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §5",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nX-Test : value\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400)
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-3-MULTI-SP-REQUEST-LINE",
            Description = "Multiple spaces between request-line components should be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §3",
            PayloadFactory = ctx => MakeRequest($"GET  / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-7.1-MISSING-HOST",
            Description = "Request without Host header must be rejected with 400",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §3.2",
            PayloadFactory = _ => MakeRequest("GET / HTTP/1.1\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400)
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-2.3-INVALID-VERSION",
            Description = "Invalid HTTP version must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §2.3",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/9.9\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode is 400 or 505)
                        return TestVerdict.Pass;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-5-EMPTY-HEADER-NAME",
            Description = "Empty header name (leading colon) must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §5",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n: empty-name\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-3-CR-ONLY-LINE-ENDING",
            Description = "CR without LF as line ending must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §2.2",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\rHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400)
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-3-MISSING-TARGET",
            Description = "Request line with no target (space but no path) must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §3",
            PayloadFactory = ctx => MakeRequest($"GET HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-3.2-FRAGMENT-IN-TARGET",
            Description = "Fragment (#) in request-target must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §3.2",
            PayloadFactory = ctx => MakeRequest($"GET /path#frag HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-2.3-HTTP09-REQUEST",
            Description = "HTTP/0.9 request (no version) must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §2.3",
            PayloadFactory = _ => MakeRequest("GET /\r\n"),
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
            Id = "RFC9112-5-INVALID-HEADER-NAME",
            Description = "Header name with invalid characters (brackets) must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §5",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nBad[Name: value\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-5-HEADER-NO-COLON",
            Description = "Header line without colon must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §5",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nNoColonHere\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "RFC9110-5.4-DUPLICATE-HOST",
            Description = "Duplicate Host headers with different values must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §3.2",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nHost: other.example.com\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400)
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-6.1-CL-NON-NUMERIC",
            Description = "Non-numeric Content-Length must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §6.1",
            PayloadFactory = ctx => MakeRequest($"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: abc\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-6.1-CL-PLUS-SIGN",
            Description = "Content-Length with plus sign must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §6.1",
            PayloadFactory = ctx => MakeRequest($"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: +5\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "COMP-WHITESPACE-BEFORE-HEADERS",
            Description = "Whitespace before first header line must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §2.2",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\n \r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "COMP-DUPLICATE-HOST-SAME",
            Description = "Duplicate Host headers with identical values must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §3.2",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400)
            }
        };

        yield return new TestCase
        {
            Id = "COMP-HOST-WITH-USERINFO",
            Description = "Host header with userinfo (user@host) must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §3.2",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: user@{ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "COMP-HOST-WITH-PATH",
            Description = "Host header with path component must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §3.2",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}/path\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "COMP-ASTERISK-WITH-GET",
            Description = "Asterisk-form (*) request-target with GET must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §3.2.4",
            PayloadFactory = ctx => MakeRequest($"GET * HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "COMP-OPTIONS-STAR",
            Description = "OPTIONS * is the only valid asterisk-form request",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §3.2.4",
            PayloadFactory = ctx => MakeRequest($"OPTIONS * HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range2xx
            }
        };

        yield return new TestCase
        {
            Id = "COMP-UNKNOWN-TE-501",
            Description = "Unknown Transfer-Encoding without CL should be rejected with 501",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §6.1",
            PayloadFactory = ctx => MakeRequest($"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: gzip\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    return response.StatusCode is 400 or 501
                        ? TestVerdict.Pass
                        : TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-LEADING-CRLF",
            Description = "Leading CRLF before request-line — server may ignore per RFC",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §2.2",
            PayloadFactory = ctx => MakeRequest($"\r\n\r\nGET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // 2xx is valid — RFC says recipient MAY ignore leading CRLF
                    return TestVerdict.Warn;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-ABSOLUTE-FORM",
            Description = "Absolute-form request-target — server should accept per RFC",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §3.2.2",
            PayloadFactory = ctx => MakeRequest($"GET http://{ctx.HostHeader}/ HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // 2xx is correct — absolute-form is valid
                    return TestVerdict.Warn;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-METHOD-CASE",
            Description = "Lowercase method 'get' — methods are case-sensitive per RFC",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9110 §9.1",
            PayloadFactory = ctx => MakeRequest($"get / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    // 400/405/501 is strict — method not recognized
                    if (response.StatusCode is 400 or 405 or 501)
                        return TestVerdict.Pass;
                    // 2xx means server treats methods case-insensitively
                    return TestVerdict.Warn;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-CONNECT-EMPTY-PORT",
            Description = "CONNECT with empty port must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §3.2.3",
            PayloadFactory = ctx => MakeRequest($"CONNECT {ctx.Host}: HTTP/1.1\r\nHost: {ctx.Host}:\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        // ── Body / Content-Length / Chunked ──────────────────────────

        yield return new TestCase
        {
            Id = "COMP-POST-CL-BODY",
            Description = "POST with Content-Length and matching body must be accepted",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §6.2",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range2xx
            }
        };

        yield return new TestCase
        {
            Id = "COMP-POST-CL-ZERO",
            Description = "POST with Content-Length: 0 and no body must be accepted",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §6.2",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range2xx,
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "COMP-POST-NO-CL-NO-TE",
            Description = "POST with neither Content-Length nor Transfer-Encoding — implicit zero-length body",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §6.3",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range2xx,
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "COMP-POST-CL-UNDERSEND",
            Description = "POST with Content-Length: 10 but only 5 bytes sent — incomplete body",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §6.2",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 10\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    // Server should wait for remaining bytes then timeout, or reject
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
            Id = "COMP-CHUNKED-BODY",
            Description = "Valid single-chunk POST must be accepted",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §7.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5\r\nhello\r\n0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range2xx
            }
        };

        yield return new TestCase
        {
            Id = "COMP-CHUNKED-MULTI",
            Description = "Valid multi-chunk POST must be accepted",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §7.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5\r\nhello\r\n6\r\n world\r\n0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range2xx
            }
        };

        yield return new TestCase
        {
            Id = "COMP-CHUNKED-EMPTY",
            Description = "Zero-length chunked body (just terminator) must be accepted",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §7.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range2xx,
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "COMP-CHUNKED-NO-FINAL",
            Description = "Chunked body without zero terminator — incomplete transfer",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §7.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5\r\nhello\r\n"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    // Server should wait for more chunks then timeout, or reject
                    if (state is ConnectionState.TimedOut or ConnectionState.ClosedByServer)
                        return TestVerdict.Pass;
                    if (response is not null && response.StatusCode == 400)
                        return TestVerdict.Pass;
                    return TestVerdict.Fail;
                }
            }
        };

        // ── Upgrade / WebSocket ─────────────────────────────────────

        yield return new TestCase
        {
            Id = "COMP-UPGRADE-POST",
            Description = "WebSocket upgrade via POST must not be accepted",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 6455 §4.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nConnection: Upgrade\r\nUpgrade: websocket\r\nSec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\nSec-WebSocket-Version: 13\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    return response.StatusCode == 101 ? TestVerdict.Fail : TestVerdict.Pass;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-UPGRADE-MISSING-CONN",
            Description = "Upgrade header without Connection: Upgrade must not trigger protocol switch",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9110 §7.8",
            PayloadFactory = ctx => MakeRequest(
                $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nUpgrade: websocket\r\nSec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\nSec-WebSocket-Version: 13\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    return response.StatusCode == 101 ? TestVerdict.Fail : TestVerdict.Pass;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-UPGRADE-UNKNOWN",
            Description = "Upgrade to unknown protocol must not return 101",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9110 §7.8",
            PayloadFactory = ctx => MakeRequest(
                $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nConnection: Upgrade\r\nUpgrade: totally-made-up/1.0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    return response.StatusCode == 101 ? TestVerdict.Fail : TestVerdict.Pass;
                }
            }
        };

        // ── Methods ─────────────────────────────────────────────────

        yield return new TestCase
        {
            Id = "COMP-METHOD-CONNECT",
            Description = "CONNECT to an origin server must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9110 §9.3.6",
            PayloadFactory = _ => MakeRequest(
                "CONNECT example.com:443 HTTP/1.1\r\nHost: example.com:443\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    return response.StatusCode is 400 or 405 or 501
                        ? TestVerdict.Pass
                        : TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-METHOD-CONNECT-NO-PORT",
            Description = "CONNECT without port in authority-form must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §3.2.3",
            PayloadFactory = _ => MakeRequest(
                "CONNECT example.com HTTP/1.1\r\nHost: example.com\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        // ── Expect ──────────────────────────────────────────────────

        yield return new TestCase
        {
            Id = "COMP-EXPECT-UNKNOWN",
            Description = "Unknown Expect value should be rejected with 417",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9110 §10.1.1",
            PayloadFactory = ctx => MakeRequest(
                $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nExpect: 200-ok\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 417)
                        return TestVerdict.Pass;
                    // Some servers ignore unknown Expect values
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        // ── Unscored ────────────────────────────────────────────────

        yield return new TestCase
        {
            Id = "COMP-GET-WITH-CL-BODY",
            Description = "GET with Content-Length and body — semantically unusual",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9110 §9.3.1",
            PayloadFactory = ctx => MakeRequest(
                $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // 2xx is RFC-compliant — GET with body is unusual but allowed
                    return TestVerdict.Warn;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-CHUNKED-EXTENSION",
            Description = "Chunk extension (valid per RFC) — server should accept or may reject",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §7.1.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5;ext=value\r\nhello\r\n0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    // 2xx = server correctly handled chunk extension
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Pass;
                    // 400 = server doesn't support extensions — warning
                    if (response.StatusCode == 400)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-UPGRADE-INVALID-VER",
            Description = "WebSocket upgrade with unsupported version — should return 426",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 6455 §4.4",
            PayloadFactory = ctx => MakeRequest(
                $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nConnection: Upgrade\r\nUpgrade: websocket\r\nSec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\nSec-WebSocket-Version: 99\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 101)
                        return TestVerdict.Fail;
                    if (response.StatusCode == 426)
                        return TestVerdict.Pass;
                    // Server doesn't support WebSocket — ignores upgrade
                    return TestVerdict.Warn;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-METHOD-TRACE",
            Description = "TRACE request — should be disabled in production",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9110 §9.3.8",
            PayloadFactory = ctx => MakeRequest(
                $"TRACE / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode is 405 or 501)
                        return TestVerdict.Pass;
                    // TRACE enabled — works but potential security concern
                    return TestVerdict.Warn;
                }
            }
        };
    }

    private static byte[] MakeRequest(string request) => Encoding.ASCII.GetBytes(request);
}
