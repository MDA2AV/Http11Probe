using System.Text;
using Http11Probe.Client;
using Http11Probe.Response;

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
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Range2xx
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-2.2-BARE-LF-REQUEST-LINE",
            Description = "Bare LF in request line should be rejected, but MAY be accepted",
            Category = TestCategory.Compliance,
            RfcLevel = RfcLevel.May,
            RfcReference = "RFC 9112 §2.2",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400 or close (pass), 2xx (warn)",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400) return TestVerdict.Pass;
                    if (response.StatusCode is >= 200 and < 300) return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-2.2-BARE-LF-HEADER",
            Description = "Bare LF in header should be rejected, but MAY be accepted",
            Category = TestCategory.Compliance,
            RfcLevel = RfcLevel.May,
            RfcReference = "RFC 9112 §2.2",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\nX-Test: value\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400 or close (pass), 2xx (warn)",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400) return TestVerdict.Pass;
                    if (response.StatusCode is >= 200 and < 300) return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
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
            Description = "Multiple spaces between request-line components — SHOULD reject but MAY parse leniently",
            Category = TestCategory.Compliance,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9112 §3",
            PayloadFactory = ctx => MakeRequest($"GET  / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400 or 2xx",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // RFC 9112 §3: recipients MAY parse on whitespace-delimited boundaries
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
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
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9112 §2.3",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/9.9\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400/505 or close",
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
            Description = "Fragment (#) in request-target — not part of origin-form grammar",
            Category = TestCategory.Compliance,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9112 §3.2",
            PayloadFactory = ctx => MakeRequest($"GET /path#frag HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400 or 2xx",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // Fragment not in origin-form grammar, but RFC only says SHOULD reject invalid request-line
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "RFC9112-2.3-HTTP09-REQUEST",
            Description = "HTTP/0.9 request (no version) must be rejected",
            Category = TestCategory.Compliance,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9112 §2.3",
            PayloadFactory = _ => MakeRequest("GET /\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400/close/timeout",
                CustomValidator = (response, state) =>
                {
                    // If server sent a response, only 400 is acceptable
                    if (response is not null)
                        return response.StatusCode == 400 ? TestVerdict.Pass : TestVerdict.Fail;
                    // No response: close or timeout means server correctly rejected
                    if (state is ConnectionState.TimedOut or ConnectionState.ClosedByServer)
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
                Description = "400/501 or close",
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
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9112 §2.2",
            PayloadFactory = ctx => MakeRequest($"\r\n\r\nGET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400 or 2xx",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-ABSOLUTE-FORM",
            Description = "Absolute-form request-target — server should accept per RFC",
            Category = TestCategory.Compliance,
            RfcLevel = RfcLevel.Should,
            Scored = false,
            RfcReference = "RFC 9112 §3.2.2",
            PayloadFactory = ctx => MakeRequest($"GET http://{ctx.HostHeader}/ HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx preferred; 400 warns",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Warn : TestVerdict.Fail;
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Pass;
                    if (response.StatusCode == 400)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-METHOD-CASE",
            Description = "Lowercase method 'get' — methods are case-sensitive per RFC",
            Category = TestCategory.Compliance,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9110 §9.1",
            PayloadFactory = ctx => MakeRequest($"get / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400/405/501 or 2xx",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode is 400 or 405 or 501)
                        return TestVerdict.Pass;
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
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
            BehavioralAnalyzer = EchoAnalyzer("hello"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx + echo",
                CustomValidator = EchoValidator("hello")
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
                Description = "400/close/timeout",
                CustomValidator = (response, state) =>
                {
                    // If server sent a response, only 400 is acceptable
                    // (a 2xx means it processed an incomplete body — always wrong)
                    if (response is not null)
                        return response.StatusCode == 400 ? TestVerdict.Pass : TestVerdict.Fail;
                    // No response: close or timeout means server correctly waited for remaining bytes
                    if (state is ConnectionState.TimedOut or ConnectionState.ClosedByServer)
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
            BehavioralAnalyzer = EchoAnalyzer("hello"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx + echo",
                CustomValidator = EchoValidator("hello")
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
            BehavioralAnalyzer = EchoAnalyzer("hello world"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx + echo",
                CustomValidator = EchoValidator("hello world")
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
                Description = "400/close/timeout",
                CustomValidator = (response, state) =>
                {
                    // If server sent a response, only 400 is acceptable
                    // (a 2xx means it processed an incomplete body — always wrong)
                    if (response is not null)
                        return response.StatusCode == 400 ? TestVerdict.Pass : TestVerdict.Fail;
                    // No response: close or timeout means server correctly waited
                    if (state is ConnectionState.TimedOut or ConnectionState.ClosedByServer)
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
                Description = "!101",
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
                Description = "!101",
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
                Description = "!101",
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
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9110 §9.3.6",
            PayloadFactory = _ => MakeRequest(
                "CONNECT example.com:443 HTTP/1.1\r\nHost: example.com:443\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400/405/501 or close",
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

        // ── Expect ──────────────────────────────────────────────────

        yield return new TestCase
        {
            Id = "COMP-EXPECT-UNKNOWN",
            Description = "Unknown Expect value should be rejected with 417",
            Category = TestCategory.Compliance,
            RfcLevel = RfcLevel.May,
            RfcReference = "RFC 9110 §10.1.1",
            PayloadFactory = ctx => MakeRequest(
                $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nExpect: 200-ok\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "417 or 2xx",
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
            RfcLevel = RfcLevel.May,
            RfcReference = "RFC 9110 §9.3.1",
            Scored = false,
            PayloadFactory = ctx => MakeRequest(
                $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                Description = "400 or 2xx",
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
            BehavioralAnalyzer = EchoAnalyzer("hello"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx preferred; 400 warns",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Warn : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Warn;
                    if (response.StatusCode is >= 200 and < 300)
                    {
                        var body = GetEffectiveBody(response);
                        if (body == "hello") return TestVerdict.Pass;
                        if (IsStaticResponse(body)) return TestVerdict.Pass;
                        return TestVerdict.Fail;
                    }
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
                Description = "non-101 (426 preferred)",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 101)
                        return TestVerdict.Fail;
                    if (response.StatusCode == 426)
                        return TestVerdict.Pass;
                    // Some servers ignore Upgrade entirely and process the GET.
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Pass;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-METHOD-TRACE",
            Description = "TRACE request — should be disabled in production",
            Category = TestCategory.Compliance,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9110 §9.3.8",
            Scored = false,
            PayloadFactory = ctx => MakeRequest(
                $"TRACE / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "405/501 or 2xx",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode is 405 or 501)
                        return TestVerdict.Pass;
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        // ── New compliance tests ─────────────────────────────────────

        yield return new TestCase
        {
            Id = "COMP-HOST-EMPTY-VALUE",
            Description = "Empty Host header value must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §3.2",
            PayloadFactory = ctx => MakeRequest("GET / HTTP/1.1\r\nHost: \r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "COMP-REQUEST-LINE-TAB",
            Description = "Tab as request-line delimiter — SHOULD reject but MAY parse on whitespace",
            Category = TestCategory.Compliance,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9112 §3",
            PayloadFactory = ctx => MakeRequest($"GET\t/ HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400 or 2xx",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-VERSION-MISSING-MINOR",
            Description = "HTTP/1 with no minor version digit is invalid",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §2.3",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "COMP-VERSION-LEADING-ZEROS",
            Description = "HTTP/01.01 — leading zeros in version digits are invalid",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §2.3",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/01.01\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "COMP-VERSION-WHITESPACE",
            Description = "HTTP/ 1.1 — whitespace inside version token is invalid",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §2.3",
            PayloadFactory = ctx => MakeRequest($"GET / HTTP/ 1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "COMP-CONNECTION-CLOSE",
            Description = "Server must close connection after responding to Connection: close",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §9.3",
            PayloadFactory = ctx => MakeRequest(
                $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nConnection: close\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx + close",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return TestVerdict.Fail;
                    if (response.StatusCode is >= 200 and < 300)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-HTTP10-DEFAULT-CLOSE",
            Description = "HTTP/1.0 without keep-alive — server should close connection after response",
            Category = TestCategory.Compliance,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9112 §9.3",
            PayloadFactory = ctx => MakeRequest(
                $"GET / HTTP/1.0\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx + close",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return TestVerdict.Fail;
                    if (response.StatusCode is >= 200 and < 300)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-HTTP10-NO-HOST",
            Description = "HTTP/1.0 without Host header — valid per HTTP/1.0",
            Category = TestCategory.Compliance,
            RfcLevel = RfcLevel.May,
            Scored = false,
            RfcReference = "RFC 9112 §3.2",
            PayloadFactory = _ => MakeRequest("GET / HTTP/1.0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "200 or 400",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-HTTP12-VERSION",
            Description = "HTTP/1.2 — higher minor version should be accepted as HTTP/1.x compatible",
            Category = TestCategory.Compliance,
            RfcLevel = RfcLevel.May,
            Scored = false,
            RfcReference = "RFC 9112 §2.3",
            PayloadFactory = ctx => MakeRequest(
                $"GET / HTTP/1.2\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "200 or 505",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode is >= 200 and < 300 or 505)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-TRACE-WITH-BODY",
            Description = "TRACE with Content-Length body should be rejected",
            Category = TestCategory.Compliance,
            RfcLevel = RfcLevel.Should,
            Scored = false,
            RfcReference = "RFC 9110 §9.3.8",
            PayloadFactory = ctx => MakeRequest(
                $"TRACE / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                Description = "400/405 or 200",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode is 400 or 405 or 501)
                        return TestVerdict.Pass;
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-CHUNKED-TRAILER-VALID",
            Description = "Valid chunked body with trailer field should be accepted",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §7.1.2",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5\r\nhello\r\n0\r\nX-Checksum: abc\r\n\r\n"),
            BehavioralAnalyzer = EchoAnalyzer("hello"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx + echo",
                CustomValidator = EchoValidator("hello")
            }
        };

        yield return new TestCase
        {
            Id = "COMP-CHUNKED-HEX-UPPERCASE",
            Description = "Chunk size with uppercase hex (A = 10) should be accepted",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §7.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\nA\r\nhelloworld\r\n0\r\n\r\n"),
            BehavioralAnalyzer = EchoAnalyzer("helloworld"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx + echo",
                CustomValidator = EchoValidator("helloworld")
            }
        };

        // ── Range / Conditional ─────────────────────────────────────

        yield return new TestCase
        {
            Id = "COMP-RANGE-POST",
            Description = "Range header on POST must be ignored — Range only applies to GET",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9110 §14.2",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\nRange: bytes=0-10\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx (Range ignored)",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Fail : TestVerdict.Fail;
                    if (response.StatusCode == 206)
                        return TestVerdict.Fail;
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Pass;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-UPGRADE-HTTP10",
            Description = "Upgrade header in HTTP/1.0 request must be ignored",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9110 §7.8",
            PayloadFactory = ctx => MakeRequest(
                $"GET / HTTP/1.0\r\nHost: {ctx.HostHeader}\r\nConnection: Upgrade\r\nUpgrade: websocket\r\nSec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\nSec-WebSocket-Version: 13\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "!101",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 101)
                        return TestVerdict.Fail;
                    return TestVerdict.Pass;
                }
            }
        };

        // ── RFC 9110 response semantics ──────────────────────────────

        yield return new TestCase
        {
            Id = "COMP-HEAD-NO-BODY",
            Description = "HEAD response must not contain a message body",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9110 §9.3.2",
            PayloadFactory = ctx => MakeRequest(
                $"HEAD / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx with no body",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Fail : TestVerdict.Fail;
                    if (response.StatusCode is >= 200 and < 300)
                        return string.IsNullOrEmpty(response.Body) ? TestVerdict.Pass : TestVerdict.Fail;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-UNKNOWN-METHOD",
            Description = "Unrecognized method should be rejected with 501 or 405",
            Category = TestCategory.Compliance,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9110 §9.1",
            PayloadFactory = ctx => MakeRequest(
                $"FOOBAR / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "501/405/400 or close",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode is 501 or 405 or 400)
                        return TestVerdict.Pass;
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-405-ALLOW",
            Description = "405 response must include an Allow header",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9110 §15.5.6",
            PayloadFactory = ctx => MakeRequest(
                $"DELETE / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "405 + Allow header",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return TestVerdict.Fail;
                    if (response.StatusCode == 405)
                        return response.Headers.ContainsKey("Allow") ? TestVerdict.Pass : TestVerdict.Fail;
                    // Server didn't return 405 — can't verify the Allow requirement
                    return TestVerdict.Warn;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-DATE-HEADER",
            Description = "Origin server must include Date header in responses",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9110 §6.6.1",
            PayloadFactory = ctx => MakeRequest(
                $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx with Date header",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return TestVerdict.Fail;
                    if (response.StatusCode is >= 200 and < 300)
                        return response.Headers.ContainsKey("Date") ? TestVerdict.Pass : TestVerdict.Fail;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-DATE-FORMAT",
            Description = "Date header should use IMF-fixdate format",
            Category = TestCategory.Compliance,
            Scored = false,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9110 §5.6.7",
            PayloadFactory = ctx => MakeRequest(
                $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "IMF-fixdate format",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return TestVerdict.Fail;
                    if (response.StatusCode is not (>= 200 and < 300))
                        return TestVerdict.Fail;
                    if (!response.Headers.TryGetValue("Date", out var date))
                        return TestVerdict.Warn;
                    // IMF-fixdate: "Sun, 06 Nov 1994 08:49:37 GMT"
                    return DateTime.TryParseExact(date.Trim(),
                        "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out _)
                        ? TestVerdict.Pass
                        : TestVerdict.Warn;
                }
            },
            BehavioralAnalyzer = response =>
            {
                if (response is null) return null;
                if (!response.Headers.TryGetValue("Date", out var date))
                    return "No Date header present";
                return DateTime.TryParseExact(date.Trim(),
                    "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out _)
                    ? $"IMF-fixdate: {date.Trim()}"
                    : $"Non-standard format: {date.Trim()}";
            }
        };

        yield return new TestCase
        {
            Id = "COMP-NO-1XX-HTTP10",
            Description = "Server must not send 1xx responses to an HTTP/1.0 client",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9110 §15.2",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.0\r\nHost: {ctx.HostHeader}\r\nExpect: 100-continue\r\nContent-Length: 5\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                Description = "non-1xx response",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state is ConnectionState.ClosedByServer or ConnectionState.TimedOut
                            ? TestVerdict.Pass
                            : TestVerdict.Fail;
                    // Server sent 100 Continue to an HTTP/1.0 client — violation
                    if (response.StatusCode is >= 100 and < 200)
                        return TestVerdict.Fail;
                    return TestVerdict.Pass;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-NO-CL-IN-204",
            Description = "Server must not send Content-Length in a 204 response",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9110 §8.6",
            PayloadFactory = ctx => MakeRequest(
                $"OPTIONS / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "204 without CL",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return TestVerdict.Fail;
                    if (response.StatusCode == 204)
                        return response.Headers.ContainsKey("Content-Length") ? TestVerdict.Fail : TestVerdict.Pass;
                    // Server didn't return 204 — can't verify the CL prohibition
                    return TestVerdict.Warn;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-OPTIONS-ALLOW",
            Description = "OPTIONS response should include Allow header listing supported methods",
            Category = TestCategory.Compliance,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9110 §9.3.7",
            PayloadFactory = ctx => MakeRequest(
                $"OPTIONS / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx with Allow header",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return TestVerdict.Fail;
                    if (response.StatusCode is >= 200 and < 300)
                        return response.Headers.ContainsKey("Allow") ? TestVerdict.Pass : TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "COMP-CONTENT-TYPE",
            Description = "Response with content should include Content-Type header",
            Category = TestCategory.Compliance,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9110 §8.3",
            PayloadFactory = ctx => MakeRequest(
                $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx with Content-Type",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return TestVerdict.Fail;
                    if (response.StatusCode is >= 200 and < 300)
                        return response.Headers.ContainsKey("Content-Type") ? TestVerdict.Pass : TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        // ── Version case-sensitivity ────────────────────────────────
        yield return new TestCase
        {
            Id = "COMP-VERSION-CASE",
            Description = "HTTP version is case-sensitive — lowercase 'http' must be rejected",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §2.3",
            PayloadFactory = ctx => MakeRequest(
                $"GET / http/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400 or close",
                AllowConnectionClose = true,
                ExpectedStatus = StatusCodeRange.Exact(400)
            }
        };

        // ── Long URL acceptance ─────────────────────────────────────
        yield return new TestCase
        {
            Id = "COMP-LONG-URL-OK",
            Description = "Server should accept request-lines of at least 8000 octets",
            Category = TestCategory.Compliance,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9112 §3",
            PayloadFactory = ctx =>
            {
                var path = "/" + new string('a', 7900);
                return MakeRequest($"GET {path} HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n");
            },
            Expected = new ExpectedBehavior
            {
                Description = "not 414",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Warn : TestVerdict.Fail;
                    if (response.StatusCode == 414)
                        return TestVerdict.Fail;
                    return TestVerdict.Pass;
                }
            }
        };

        // ── Space inside request-target ─────────────────────────────
        yield return new TestCase
        {
            Id = "COMP-SPACE-IN-TARGET",
            Description = "Whitespace inside request-target is invalid",
            Category = TestCategory.Compliance,
            RfcReference = "RFC 9112 §3.2",
            PayloadFactory = ctx => MakeRequest(
                $"GET /pa th HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400 or close",
                AllowConnectionClose = true,
                ExpectedStatus = StatusCodeRange.Exact(400)
            }
        };

        // ── Duplicate Content-Type ──────────────────────────────────
        yield return new TestCase
        {
            Id = "COMP-DUPLICATE-CT",
            Description = "Duplicate Content-Type headers with different values",
            Category = TestCategory.Compliance,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9110 §5.3",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\nContent-Type: text/plain\r\nContent-Type: text/html\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                Description = "400 or 2xx",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        // ── TRACE with sensitive headers ────────────────────────────
        yield return new TestCase
        {
            Id = "COMP-TRACE-SENSITIVE",
            Description = "TRACE should exclude sensitive headers from echoed response",
            Category = TestCategory.Compliance,
            Scored = false,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9110 §9.3.8",
            PayloadFactory = ctx => MakeRequest(
                $"TRACE / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nAuthorization: Bearer secret-token-123\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "405/501, or 200 without Auth",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    // TRACE disabled — good
                    if (response.StatusCode is 405 or 501)
                        return TestVerdict.Pass;
                    if (response.StatusCode is >= 200 and < 300)
                    {
                        // Check if the echoed body contains the Authorization value
                        var body = response.Body ?? "";
                        if (body.Contains("secret-token-123", StringComparison.OrdinalIgnoreCase))
                            return TestVerdict.Warn;
                        return TestVerdict.Pass;
                    }
                    return TestVerdict.Fail;
                }
            },
            BehavioralAnalyzer = response =>
            {
                if (response is null) return null;
                if (response.StatusCode is 405 or 501)
                    return "TRACE disabled — sensitive headers not exposed";
                if (response.StatusCode is >= 200 and < 300)
                {
                    var body = response.Body ?? "";
                    return body.Contains("secret-token-123", StringComparison.OrdinalIgnoreCase)
                        ? "TRACE echoed Authorization header — sensitive data exposed"
                        : "TRACE response excludes Authorization header — safe";
                }
                return null;
            }
        };

        // ── Invalid Range syntax ────────────────────────────────────
        yield return new TestCase
        {
            Id = "COMP-RANGE-INVALID",
            Description = "Invalid Range header syntax should be ignored",
            Category = TestCategory.Compliance,
            Scored = false,
            RfcLevel = RfcLevel.May,
            RfcReference = "RFC 9110 §14.2",
            PayloadFactory = ctx => MakeRequest(
                $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nRange: bytes=abc-xyz\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "200 or 416",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Warn : TestVerdict.Fail;
                    // Server ignored invalid Range — returned full content
                    if (response.StatusCode is >= 200 and < 300 && response.StatusCode != 206)
                        return TestVerdict.Pass;
                    // Server explicitly rejected invalid Range
                    if (response.StatusCode == 416)
                        return TestVerdict.Pass;
                    return TestVerdict.Warn;
                }
            }
        };

        // ── Unrecognized Accept value ───────────────────────────────
        yield return new TestCase
        {
            Id = "COMP-ACCEPT-NONSENSE",
            Description = "Unrecognized Accept value — server may return 406 or default representation",
            Category = TestCategory.Compliance,
            Scored = false,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9110 §12.5.1",
            PayloadFactory = ctx => MakeRequest(
                $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nAccept: application/x-nonsense\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "406 or 2xx",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Warn : TestVerdict.Fail;
                    if (response.StatusCode == 406)
                        return TestVerdict.Pass;
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            },
            BehavioralAnalyzer = response =>
            {
                if (response is null) return null;
                if (response.StatusCode == 406)
                    return "Server enforces Accept negotiation — returned 406";
                if (response.StatusCode is >= 200 and < 300)
                    return "Server ignored unrecognized Accept — returned default representation";
                return null;
            }
        };

        // ── Unsupported Content-Type on POST ────────────────────────
        yield return new TestCase
        {
            Id = "COMP-POST-UNSUPPORTED-CT",
            Description = "POST with unrecognized Content-Type — server may return 415",
            Category = TestCategory.Compliance,
            Scored = false,
            RfcLevel = RfcLevel.May,
            RfcReference = "RFC 9110 §15.5.16",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\nContent-Type: application/x-nonsense\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                Description = "415 or 2xx",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Warn : TestVerdict.Fail;
                    if (response.StatusCode == 415)
                        return TestVerdict.Pass;
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Pass;
                    return TestVerdict.Warn;
                }
            },
            BehavioralAnalyzer = response =>
            {
                if (response is null) return null;
                if (response.StatusCode == 415)
                    return "Server enforces Content-Type validation — returned 415";
                if (response.StatusCode is >= 200 and < 300)
                    return "Server accepted unknown Content-Type";
                return null;
            }
        };
    }

    // ── Echo verification helpers ──────────────────────────────

    private static bool IsStaticResponse(string body) => body == "OK";

    /// <summary>
    /// Extracts the effective body from a response, decoding chunked TE if present.
    /// </summary>
    private static string GetEffectiveBody(HttpResponse response)
    {
        var raw = response.Body ?? "";

        if (response.Headers.TryGetValue("Transfer-Encoding", out var te) &&
            te.Contains("chunked", StringComparison.OrdinalIgnoreCase))
        {
            var decoded = TryDecodeChunked(raw);
            if (decoded is not null)
                return decoded;
        }

        return raw.TrimEnd('\r', '\n');
    }

    private static string? TryDecodeChunked(string raw)
    {
        var sb = new StringBuilder();
        var pos = 0;

        while (pos < raw.Length)
        {
            var lineEnd = raw.IndexOf("\r\n", pos, StringComparison.Ordinal);
            if (lineEnd < 0) return null;

            var sizeLine = raw[pos..lineEnd];
            var semiIdx = sizeLine.IndexOf(';');
            if (semiIdx >= 0) sizeLine = sizeLine[..semiIdx];
            sizeLine = sizeLine.Trim();

            if (!int.TryParse(sizeLine, System.Globalization.NumberStyles.HexNumber, null, out var chunkSize))
                return null;

            if (chunkSize == 0)
                break;

            var dataStart = lineEnd + 2;
            var dataEnd = dataStart + chunkSize;
            if (dataEnd > raw.Length) return null;

            sb.Append(raw[dataStart..dataEnd]);

            // Skip past chunk data + CRLF
            pos = dataEnd;
            if (pos + 2 <= raw.Length && raw[pos] == '\r' && raw[pos + 1] == '\n')
                pos += 2;
            else if (pos < raw.Length)
                pos++;
        }

        return sb.ToString();
    }

    private static Func<HttpResponse?, string?> EchoAnalyzer(string expectedBody)
    {
        return response =>
        {
            if (response is null || response.StatusCode is < 200 or >= 300) return null;
            var body = GetEffectiveBody(response);
            if (IsStaticResponse(body)) return "Static response — server does not echo POST body";
            if (body == expectedBody) return "Echoed correctly";
            if (body.Length == 0) return "Empty body — server did not echo";
            return $"Echo mismatch: expected \"{expectedBody}\", got \"{(body.Length > 40 ? body[..40] + "..." : body)}\"";
        };
    }

    private static Func<HttpResponse?, ConnectionState, TestVerdict> EchoValidator(string expectedBody)
    {
        return (response, state) =>
        {
            if (response is null)
                return TestVerdict.Fail;
            if (response.StatusCode is < 200 or >= 300)
                return TestVerdict.Fail;
            var body = GetEffectiveBody(response);
            if (body == expectedBody) return TestVerdict.Pass;
            if (IsStaticResponse(body)) return TestVerdict.Pass;
            return TestVerdict.Fail;
        };
    }

    private static byte[] MakeRequest(string request) => Encoding.ASCII.GetBytes(request);
}
