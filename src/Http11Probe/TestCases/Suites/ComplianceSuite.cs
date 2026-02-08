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
    }

    private static byte[] MakeRequest(string request) => Encoding.ASCII.GetBytes(request);
}
