using System.Text;
using Http11Probe.Client;
using Http11Probe.Response;

namespace Http11Probe.TestCases.Suites;

public static class SmugglingSuite
{
    // ── Behavioral analyzers ────────────────────────────────────
    // Examine the echoed body to determine which framing the server used.
    // Static-config servers (Nginx, Apache, etc.) always return "OK" and cannot echo.

    private const string StaticNote = "Static response — server does not echo POST body";

    private static bool IsStaticResponse(string body) => body == "OK";

    private static string? AnalyzeClTeBoth(HttpResponse? r)
    {
        if (r is null || r.StatusCode is < 200 or >= 300) return null;
        var body = (r.Body ?? "").TrimEnd('\r', '\n');
        if (IsStaticResponse(body)) return StaticNote;
        if (body.Length == 0) return "Used TE (chunked 0-length → empty body)";
        if (body.Contains("0\r\n\r\n") || body == "0\r\n\r") return "Used CL (read 6 raw bytes including chunk terminator)";
        return $"Body: {Truncate(body)}";
    }

    private static string? AnalyzeDuplicateCl(HttpResponse? r)
    {
        // Payload: "helloworld" with CL:5 and CL:10
        // CL:5 → "hello", CL:10 → "helloworld"
        if (r is null || r.StatusCode is < 200 or >= 300) return null;
        var body = (r.Body ?? "").TrimEnd('\r', '\n');
        if (IsStaticResponse(body)) return StaticNote;
        if (body == "hello") return "Used first CL (5 bytes)";
        if (body == "helloworld") return "Used second CL (10 bytes)";
        if (body.Length == 0) return "Empty body (server consumed no body)";
        return $"Body: {Truncate(body)}";
    }

    private static string? AnalyzeTeWithClFallback(HttpResponse? r)
    {
        // Tests with TE variant + CL:5 + body "hello"
        // If server used CL → body is "hello"; if TE recognized → empty (chunked parse of "hello")
        if (r is null || r.StatusCode is < 200 or >= 300) return null;
        var body = (r.Body ?? "").TrimEnd('\r', '\n');
        if (IsStaticResponse(body)) return StaticNote;
        if (body == "hello") return "Used CL (ignored TE variant)";
        if (body.Length == 0) return "Used TE (treated as chunked)";
        return $"Body: {Truncate(body)}";
    }

    private static string Truncate(string s) => s.Length > 40 ? s[..40] + "..." : s;

    public static IEnumerable<TestCase> GetTestCases()
    {
        yield return new TestCase
        {
            Id = "SMUG-CL-TE-BOTH",
            Description = "Both Content-Length and Transfer-Encoding present — server MAY reject or process with TE alone",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 6\r\nTransfer-Encoding: chunked\r\n\r\n0\r\n\r\n"),
            BehavioralAnalyzer = AnalyzeClTeBoth,
            Expected = new ExpectedBehavior
            {
                Description = "400 or 2xx",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // RFC 9112 §6.3: server MAY process with TE alone
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-DUPLICATE-CL",
            Description = "Duplicate Content-Length with different values must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §8.6",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\nContent-Length: 10\r\n\r\nhelloworld"),
            BehavioralAnalyzer = AnalyzeDuplicateCl,
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CL-LEADING-ZEROS",
            Description = "Content-Length with leading zeros — valid per 1*DIGIT grammar but may cause parser disagreement",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §8.6",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 005\r\n\r\nhello"),
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
            Id = "SMUG-TE-XCHUNKED",
            Description = "Transfer-Encoding: xchunked must not be treated as chunked",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: xchunked\r\nContent-Length: 5\r\n\r\nhello"),
            BehavioralAnalyzer = AnalyzeTeWithClFallback,
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
            Id = "SMUG-TE-TRAILING-SPACE",
            Description = "Transfer-Encoding: 'chunked ' (trailing space) must not be treated as chunked",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked \r\nContent-Length: 5\r\n\r\nhello"),
            BehavioralAnalyzer = AnalyzeTeWithClFallback,
            Expected = new ExpectedBehavior
            {
                Description = "400/501 or 2xx+close",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;

                    if (response.StatusCode is 400 or 501)
                        return TestVerdict.Pass;

                    // If recipient trims OWS and recognizes chunked, RFC allows processing;
                    // with CL+TE present, connection should be closed after response.
                    if (response.StatusCode is >= 200 and < 300)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Warn : TestVerdict.Fail;

                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-TE-SP-BEFORE-COLON",
            Description = "Transfer-Encoding with space before colon must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §5",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding : chunked\r\nContent-Length: 5\r\n\r\nhello"),
            BehavioralAnalyzer = AnalyzeTeWithClFallback,
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CL-NEGATIVE",
            Description = "Negative Content-Length must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §8.6",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: -1\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CLTE-PIPELINE",
            Description = "CL.TE smuggling probe — follow-up should not receive smuggled response",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            RequiresConnectionReuse = true,
            PayloadFactory = ctx =>
            {
                // Ambiguous: CL says body is 4 bytes ("0\r\n\r"), but TE chunked says 0 chunk = end
                // A CL-only parser reads 4 bytes and waits; a TE parser sees end-of-chunks
                var body = $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 4\r\nTransfer-Encoding: chunked\r\n\r\n0\r\n\r\n";
                return Encoding.ASCII.GetBytes(body);
            },
            FollowUpPayloadFactory = ctx =>
                Encoding.ASCII.GetBytes($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400 or close",
                CustomValidator = (response, state) =>
                {
                    // Best: server rejects the ambiguous request with 400
                    if (response is not null && response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // Connection closed = safe
                    if (state == ConnectionState.ClosedByServer)
                        return TestVerdict.Pass;
                    // If we got a 2xx, might be vulnerable — fail
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-TECL-PIPELINE",
            Description = "TE.CL smuggling probe — TE: chunked + CL: 30 with pipelined GET",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            RequiresConnectionReuse = true,
            PayloadFactory = ctx =>
            {
                // TE.CL reverse: TE parser sees chunked body, CL parser reads 30 bytes
                var body = $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\nContent-Length: 30\r\n\r\n0\r\n\r\n";
                return Encoding.ASCII.GetBytes(body);
            },
            FollowUpPayloadFactory = ctx =>
                Encoding.ASCII.GetBytes($"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400 or close",
                CustomValidator = (response, state) =>
                {
                    if (response is not null && response.StatusCode == 400)
                        return TestVerdict.Pass;
                    if (state == ConnectionState.ClosedByServer)
                        return TestVerdict.Pass;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CL-TRAILING-SPACE",
            Description = "Content-Length with trailing space — OWS trimming is valid per RFC 9110 §5.5",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §5.5",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5 \r\n\r\nhello"),
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
            Id = "SMUG-TE-DOUBLE-CHUNKED",
            Description = "Transfer-Encoding: chunked, chunked with CL is ambiguous",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked, chunked\r\nContent-Length: 5\r\n\r\nhello"),
            BehavioralAnalyzer = AnalyzeTeWithClFallback,
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
            Id = "SMUG-CL-EXTRA-LEADING-SP",
            Description = "Content-Length with extra leading whitespace (double space OWS)",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §5.5",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length:  5\r\n\r\nhello"),
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
            Id = "SMUG-TE-CASE-MISMATCH",
            Description = "Transfer-Encoding: Chunked (capital C) with CL — case-insensitive is valid",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: Chunked\r\nContent-Length: 5\r\n\r\nhello"),
            BehavioralAnalyzer = AnalyzeTeWithClFallback,
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

        // ── Critical: Scored ──────────────────────────────────────────

        yield return new TestCase
        {
            Id = "SMUG-CL-COMMA-DIFFERENT",
            Description = "Content-Length with comma-separated different values must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §8.6",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5, 10\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-TE-NOT-FINAL-CHUNKED",
            Description = "Transfer-Encoding where chunked is not final — server MUST respond with 400 (RFC 9112 §6.3)",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.3",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked, gzip\r\n\r\n0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-TE-HTTP10",
            Description = "Transfer-Encoding in HTTP/1.0 request must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.0\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\nContent-Length: 5\r\n\r\nhello"),
            BehavioralAnalyzer = AnalyzeTeWithClFallback,
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CHUNK-BARE-SEMICOLON",
            Description = "Chunk size with bare semicolon and no extension name must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §7.1.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5;\r\nhello\r\n0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-BARE-CR-HEADER-VALUE",
            Description = "Bare CR in header value must be rejected or replaced with SP",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §2.2",
            PayloadFactory = ctx =>
            {
                var request = $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\nX-Test: val\rue\r\n\r\nhello";
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
            Id = "SMUG-CL-OCTAL",
            Description = "Content-Length with octal prefix (0o5) must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §8.6",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 0o5\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CHUNK-UNDERSCORE",
            Description = "Chunk size with underscores (1_0) must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §7.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n1_0\r\nhello world!!!!!\r\n0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-TE-EMPTY-VALUE",
            Description = "Transfer-Encoding with empty value must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: \r\nContent-Length: 5\r\n\r\nhello"),
            BehavioralAnalyzer = AnalyzeTeWithClFallback,
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-TE-LEADING-COMMA",
            Description = "Transfer-Encoding with leading comma (, chunked) — RFC says empty list elements MUST be ignored",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §5.6.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: , chunked\r\nContent-Length: 5\r\n\r\nhello"),
            BehavioralAnalyzer = AnalyzeTeWithClFallback,
            Expected = new ExpectedBehavior
            {
                Description = "400 or 2xx",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // RFC 9110 §5.6.1: MUST ignore empty list elements — 2xx is compliant
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-TE-DUPLICATE-HEADERS",
            Description = "Two Transfer-Encoding headers with CL present — ambiguous framing",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\nTransfer-Encoding: identity\r\nContent-Length: 5\r\n\r\nhello"),
            BehavioralAnalyzer = AnalyzeTeWithClFallback,
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CHUNK-HEX-PREFIX",
            Description = "Chunk size with 0x prefix must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §7.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n0x5\r\nhello\r\n0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CL-HEX-PREFIX",
            Description = "Content-Length with hex prefix (0x5) must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §8.6",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 0x5\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CL-INTERNAL-SPACE",
            Description = "Content-Length with internal space (1 0) must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §8.6",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 1 0\r\n\r\nhello12345"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CHUNK-LEADING-SP",
            Description = "Chunk size with leading space must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §7.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n 5\r\nhello\r\n0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CHUNK-MISSING-TRAILING-CRLF",
            Description = "Chunk data without trailing CRLF must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §7.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5\r\nhello0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        // ── Funky chunks (2024–2025 research) ───────────────────────

        yield return new TestCase
        {
            Id = "SMUG-CHUNK-EXT-LF",
            Description = "Bare LF in chunk extension — server MAY accept bare LF per RFC 9112 §2.2",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §7.1.1",
            PayloadFactory = ctx =>
            {
                // Chunk line: "5;\n" — bare LF in extension area
                var request = $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5;\nhello\r\n0\r\n\r\n";
                return Encoding.ASCII.GetBytes(request);
            },
            Expected = new ExpectedBehavior
            {
                Description = "400 or 2xx",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // RFC 9112 §2.2: MAY recognize bare LF as line terminator
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CHUNK-SPILL",
            Description = "Chunk declares size 5 but sends 7 bytes — oversized chunk data must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §7.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5\r\nhello!!\r\n0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CHUNK-LF-TERM",
            Description = "Bare LF as chunk data terminator — server MAY accept bare LF per RFC 9112 §2.2",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §7.1",
            PayloadFactory = ctx =>
            {
                // Chunk data terminated with \n instead of \r\n
                var request = $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5\r\nhello\n0\r\n\r\n";
                return Encoding.ASCII.GetBytes(request);
            },
            Expected = new ExpectedBehavior
            {
                Description = "400 or 2xx",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // RFC 9112 §2.2: MAY recognize bare LF as line terminator
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CHUNK-EXT-CTRL",
            Description = "NUL byte in chunk extension must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §7.1.1",
            PayloadFactory = ctx =>
            {
                var before = Encoding.ASCII.GetBytes($"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5;");
                byte[] nul = [0x00];
                var after = Encoding.ASCII.GetBytes("ext\r\nhello\r\n0\r\n\r\n");
                var payload = new byte[before.Length + nul.Length + after.Length];
                before.CopyTo(payload, 0);
                nul.CopyTo(payload, before.Length);
                after.CopyTo(payload, before.Length + nul.Length);
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
            Id = "SMUG-CHUNK-EXT-CR",
            Description = "Bare CR (not CRLF) in chunk extension — some parsers treat CR alone as line ending",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §7.1.1",
            PayloadFactory = ctx =>
            {
                // "5;a\rX\r\n" — the \r after "a" is NOT followed by \n
                var before = Encoding.ASCII.GetBytes($"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5;a");
                byte[] bareCr = [0x0d]; // bare CR
                var after = Encoding.ASCII.GetBytes("X\r\nhello\r\n0\r\n\r\n");
                var payload = new byte[before.Length + bareCr.Length + after.Length];
                before.CopyTo(payload, 0);
                bareCr.CopyTo(payload, before.Length);
                after.CopyTo(payload, before.Length + bareCr.Length);
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
            Id = "SMUG-TE-VTAB",
            Description = "Vertical tab before 'chunked' in TE value — control char obfuscation vector",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            PayloadFactory = ctx =>
            {
                var before = Encoding.ASCII.GetBytes($"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: ");
                byte[] vtab = [0x0b];
                var after = Encoding.ASCII.GetBytes("chunked\r\nContent-Length: 5\r\n\r\nhello");
                var payload = new byte[before.Length + vtab.Length + after.Length];
                before.CopyTo(payload, 0);
                vtab.CopyTo(payload, before.Length);
                after.CopyTo(payload, before.Length + vtab.Length);
                return payload;
            },
            BehavioralAnalyzer = AnalyzeTeWithClFallback,
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-TE-FORMFEED",
            Description = "Form feed before 'chunked' in TE value — control char obfuscation vector",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            PayloadFactory = ctx =>
            {
                var before = Encoding.ASCII.GetBytes($"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: ");
                byte[] ff = [0x0c];
                var after = Encoding.ASCII.GetBytes("chunked\r\nContent-Length: 5\r\n\r\nhello");
                var payload = new byte[before.Length + ff.Length + after.Length];
                before.CopyTo(payload, 0);
                ff.CopyTo(payload, before.Length);
                after.CopyTo(payload, before.Length + ff.Length);
                return payload;
            },
            BehavioralAnalyzer = AnalyzeTeWithClFallback,
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-TE-NULL",
            Description = "NUL byte appended to 'chunked' in TE value — C-string truncation attack",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            PayloadFactory = ctx =>
            {
                var before = Encoding.ASCII.GetBytes($"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked");
                byte[] nul = [0x00];
                var after = Encoding.ASCII.GetBytes("\r\nContent-Length: 5\r\n\r\nhello");
                var payload = new byte[before.Length + nul.Length + after.Length];
                before.CopyTo(payload, 0);
                nul.CopyTo(payload, before.Length);
                after.CopyTo(payload, before.Length + nul.Length);
                return payload;
            },
            BehavioralAnalyzer = AnalyzeTeWithClFallback,
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CHUNK-LF-TRAILER",
            Description = "Bare LF in chunked trailer termination — server MAY accept bare LF per RFC 9112 §2.2",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §7.1",
            PayloadFactory = ctx =>
            {
                // Last CRLF of trailer replaced with bare LF: "0\r\n\n"
                var request = $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5\r\nhello\r\n0\r\n\n";
                return Encoding.ASCII.GetBytes(request);
            },
            Expected = new ExpectedBehavior
            {
                Description = "400 or 2xx",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // RFC 9112 §2.2: MAY recognize bare LF as line terminator
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-TE-IDENTITY",
            Description = "Transfer-Encoding: identity (deprecated) with CL must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §7",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: identity\r\nContent-Length: 5\r\n\r\nhello"),
            BehavioralAnalyzer = AnalyzeTeWithClFallback,
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
            Id = "SMUG-CHUNK-NEGATIVE",
            Description = "Negative chunk size must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §7.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n-1\r\nhello\r\n0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        // ── Unscored ──────────────────────────────────────────────────

        yield return new TestCase
        {
            Id = "SMUG-TRANSFER_ENCODING",
            Description = "Transfer_Encoding (underscore) header with CL — not a valid header but some parsers accept",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            Scored = false,
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer_Encoding: chunked\r\nContent-Length: 5\r\n\r\nhello"),
            BehavioralAnalyzer = AnalyzeTeWithClFallback,
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
            Id = "SMUG-CL-COMMA-SAME",
            Description = "Content-Length with comma-separated identical values — some servers merge",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §8.6",
            Scored = false,
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5, 5\r\n\r\nhello"),
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
            Id = "SMUG-CHUNKED-WITH-PARAMS",
            Description = "Transfer-Encoding: chunked;ext=val — parameters on chunked encoding",
            Category = TestCategory.Smuggling,
            Scored = false,
            RfcReference = "RFC 9112 §7",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked;ext=val\r\nContent-Length: 5\r\n\r\nhello"),
            BehavioralAnalyzer = AnalyzeTeWithClFallback,
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
            Id = "SMUG-EXPECT-100-CL",
            Description = "Expect: 100-continue with Content-Length — server should send 100 then read body",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §10.1.1",
            Scored = false,
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\nExpect: 100-continue\r\n\r\nhello"),
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
            Id = "SMUG-TRAILER-CL",
            Description = "Content-Length in chunked trailers must be ignored — prohibited trailer field",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §6.5.1",
            Scored = false,
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5\r\nhello\r\n0\r\nContent-Length: 50\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "400 or 2xx",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // 2xx = server processed chunked body and ignored trailer CL
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-TRAILER-TE",
            Description = "Transfer-Encoding in chunked trailers must be ignored — prohibited trailer field",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §6.5.1",
            Scored = false,
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5\r\nhello\r\n0\r\nTransfer-Encoding: chunked\r\n\r\n"),
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
            Id = "SMUG-TRAILER-HOST",
            Description = "Host header in chunked trailers must not be used for routing",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §6.5.2",
            Scored = false,
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5\r\nhello\r\n0\r\nHost: evil.example.com\r\n\r\n"),
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
            Id = "SMUG-TRAILER-AUTH",
            Description = "Authorization header in chunked trailers — prohibited per RFC 9110 §6.5.1",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §6.5.1",
            Scored = false,
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5\r\nhello\r\n0\r\nAuthorization: Bearer evil\r\n\r\n"),
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
            Id = "SMUG-HEAD-CL-BODY",
            Description = "HEAD request with Content-Length and body — server must not leave body on connection",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §9.3.2",
            Scored = false,
            PayloadFactory = ctx => MakeRequest(
                $"HEAD / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\n\r\nhello"),
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
            Id = "SMUG-OPTIONS-CL-BODY",
            Description = "OPTIONS with Content-Length and body — server should consume or reject body",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §9.3.7",
            Scored = false,
            PayloadFactory = ctx => MakeRequest(
                $"OPTIONS / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\n\r\nhello"),
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

        // ── New smuggling tests ──────────────────────────────────────

        yield return new TestCase
        {
            Id = "SMUG-CL-UNDERSCORE",
            Description = "Content-Length with underscore digit separator (1_0) must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §8.6",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 1_0\r\n\r\nhelloworld"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CL-NEGATIVE-ZERO",
            Description = "Content-Length: -0 must be rejected — not valid 1*DIGIT",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §8.6",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: -0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CL-DOUBLE-ZERO",
            Description = "Content-Length: 00 — matches 1*DIGIT but leading zero ambiguity",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §8.6",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 00\r\n\r\n"),
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
            Id = "SMUG-CL-LEADING-ZEROS-OCTAL",
            Description = "Content-Length: 0200 — octal 128 vs decimal 200, parser disagreement vector",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §8.6",
            PayloadFactory = ctx =>
            {
                // Send exactly 200 bytes of body — if server reads 128 (octal), 72 bytes leak
                var body = new string('A', 200);
                return MakeRequest(
                    $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 0200\r\n\r\n{body}");
            },
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
            Id = "SMUG-TE-OBS-FOLD",
            Description = "Transfer-Encoding with obs-fold line wrapping must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §5.2",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding:\r\n chunked\r\nContent-Length: 5\r\n\r\nhello"),
            BehavioralAnalyzer = AnalyzeTeWithClFallback,
            Expected = new ExpectedBehavior
            {
                Description = "400 or 2xx+close",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;

                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;

                    // RFC 9112 §5.2 permits unfolding obs-fold; if unfolded to TE+CL,
                    // RFC 9112 §6.1 requires closing the connection after responding.
                    if (response.StatusCode is >= 200 and < 300)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Warn : TestVerdict.Fail;

                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-TE-TRAILING-COMMA",
            Description = "Transfer-Encoding: chunked, — trailing comma produces empty list element",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §5.6.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked,\r\nContent-Length: 5\r\n\r\nhello"),
            BehavioralAnalyzer = AnalyzeTeWithClFallback,
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
            Id = "SMUG-TE-TAB-BEFORE-VALUE",
            Description = "Transfer-Encoding with tab as OWS before value",
            Category = TestCategory.Smuggling,
            Scored = false,
            RfcReference = "RFC 9110 §5.5",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding:\tchunked\r\nContent-Length: 5\r\n\r\nhello"),
            BehavioralAnalyzer = AnalyzeTeWithClFallback,
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
            Id = "SMUG-ABSOLUTE-URI-HOST-MISMATCH",
            Description = "Absolute-form URI with different Host header — routing confusion vector",
            Category = TestCategory.Smuggling,
            Scored = false,
            RfcReference = "RFC 9112 §3.2.2",
            PayloadFactory = ctx => MakeRequest(
                $"GET http://other.example.com/ HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n"),
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
            Id = "SMUG-MULTIPLE-HOST-COMMA",
            Description = "Host header with comma-separated values must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §7.2",
            PayloadFactory = ctx => MakeRequest(
                $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}, other.example.com\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CHUNK-BARE-CR-TERM",
            Description = "Chunk size line terminated by bare CR — not a valid line terminator",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §2.2",
            PayloadFactory = ctx =>
            {
                // 5\r hello\r\n 0\r\n \r\n — bare CR after chunk size
                var before = Encoding.ASCII.GetBytes($"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5\r");
                var after = Encoding.ASCII.GetBytes("hello\r\n0\r\n\r\n");
                var payload = new byte[before.Length + after.Length];
                before.CopyTo(payload, 0);
                after.CopyTo(payload, before.Length);
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
            Id = "SMUG-TRAILER-CONTENT-TYPE",
            Description = "Content-Type in chunked trailer — prohibited per RFC 9110 §6.5.1",
            Category = TestCategory.Smuggling,
            Scored = false,
            RfcReference = "RFC 9110 §6.5.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5\r\nhello\r\n0\r\nContent-Type: text/evil\r\n\r\n"),
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
    }

    private static byte[] MakeRequest(string request) => Encoding.ASCII.GetBytes(request);
}
