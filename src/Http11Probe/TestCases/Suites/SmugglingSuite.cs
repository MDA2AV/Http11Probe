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
        if (body == "hello-bananas") return "Used TE (decoded chunked body)";
        if (body.StartsWith("D\r\nhello-bananas", StringComparison.Ordinal)) return "Used CL (read raw chunked framing as body)";
        if (body.Length == 0) return "Empty body (server consumed no body)";
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

    private static bool IsDigit(char c) => c is >= '0' and <= '9';

    // Detect multiple HTTP responses in a single read buffer (e.g., when embedded request bytes are parsed as a second request).
    // We intentionally match the status line prefix pattern rather than "HTTP/" anywhere to avoid
    // false-positives from echoed request bodies like "GET / HTTP/1.1".
    private static int CountHttpStatusLines(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return 0;

        var count = 0;
        var idx = 0;

        while (true)
        {
            idx = raw.IndexOf("HTTP/", idx, StringComparison.Ordinal);
            if (idx < 0) break;

            // Minimal pattern: HTTP/x.y SSS
            if (idx + 12 <= raw.Length
                && IsDigit(raw[idx + 5])
                && raw[idx + 6] == '.'
                && IsDigit(raw[idx + 7])
                && raw[idx + 8] == ' '
                && IsDigit(raw[idx + 9])
                && IsDigit(raw[idx + 10])
                && IsDigit(raw[idx + 11]))
            {
                count++;
            }

            idx += 5;
        }

        return count;
    }

    public static IEnumerable<TestCase> GetTestCases()
    {
        yield return new TestCase
        {
            Id = "SMUG-CL-TE-BOTH",
            Description = "Both Content-Length and Transfer-Encoding present — server MAY reject or process with TE alone",
            Category = TestCategory.Smuggling,
            RfcLevel = RfcLevel.OughtTo,
            RfcReference = "RFC 9112 §6.1",
            PayloadFactory = ctx =>
            {
                // Non-empty chunked body so echo-capable servers reveal whether they decoded TE or used CL.
                const string chunkedBody = "D\r\nhello-bananas\r\n0\r\n\r\n";
                var cl = Encoding.ASCII.GetByteCount(chunkedBody);
                return MakeRequest(
                    $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: {cl}\r\nTransfer-Encoding: chunked\r\n\r\n{chunkedBody}");
            },
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
            RfcLevel = RfcLevel.Should,
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
            Description = "CL.TE conflict — both Content-Length and Transfer-Encoding: chunked present",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            RfcLevel = RfcLevel.May,
            PayloadFactory = ctx =>
            {
                // Ambiguous: CL says body is 4 bytes ("0\r\n\r"), but TE chunked says 0 chunk = end
                var body = $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 4\r\nTransfer-Encoding: chunked\r\n\r\n0\r\n\r\n";
                return Encoding.ASCII.GetBytes(body);
            },
            Expected = new ExpectedBehavior
            {
                Description = "400 or close preferred; 2xx acceptable",
                CustomValidator = (response, state) =>
                {
                    if (response is not null && response.StatusCode == 400)
                        return TestVerdict.Pass;
                    if (state == ConnectionState.ClosedByServer)
                        return TestVerdict.Pass;
                    if (response is not null && response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-TECL-PIPELINE",
            Description = "TE.CL conflict — Transfer-Encoding: chunked + conflicting Content-Length",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            RfcLevel = RfcLevel.May,
            PayloadFactory = ctx =>
            {
                // TE.CL reverse: TE parser sees chunked body, CL parser reads 30 bytes
                var body = $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\nContent-Length: 30\r\n\r\n0\r\n\r\n";
                return Encoding.ASCII.GetBytes(body);
            },
            Expected = new ExpectedBehavior
            {
                Description = "400 or close preferred; 2xx acceptable",
                CustomValidator = (response, state) =>
                {
                    if (response is not null && response.StatusCode == 400)
                        return TestVerdict.Pass;
                    if (state == ConnectionState.ClosedByServer)
                        return TestVerdict.Pass;
                    if (response is not null && response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Warn;
                    return TestVerdict.Fail;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CL-TRAILING-SPACE",
            Description = "Content-Length with trailing space — OWS trimming is valid per RFC 9110 §5.5",
            Category = TestCategory.Smuggling,
            RfcLevel = RfcLevel.Should,
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
            RfcLevel = RfcLevel.Should,
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
            RfcLevel = RfcLevel.Should,
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
            RfcLevel = RfcLevel.Should,
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
            Id = "SMUG-CHUNK-EXT-INVALID-TOKEN",
            Description = "Chunk extension with invalid token character must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §7.1.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5;bad[=x\r\nhello\r\n0\r\n\r\n"),
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
            RfcLevel = RfcLevel.Should,
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
            Id = "SMUG-CHUNK-SIZE-PLUS",
            Description = "Chunk size with leading plus sign must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §7.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n+5\r\nhello\r\n0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CHUNK-SIZE-TRAILING-OWS",
            Description = "Chunk size with trailing whitespace before CRLF must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §7.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5 \r\nhello\r\n0\r\n\r\n"),
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
            RfcLevel = RfcLevel.May,
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
            RfcLevel = RfcLevel.May,
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
            RfcLevel = RfcLevel.May,
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
            Id = "SMUG-CL-COMMA-TRIPLE",
            Description = "Content-Length with three comma-separated identical values — extended merge test",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §8.6",
            Scored = false,
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5, 5, 5\r\n\r\nhello"),
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
            RfcLevel = RfcLevel.Should,
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
            RfcLevel = RfcLevel.Should,
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
            RfcLevel = RfcLevel.Should,
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

    // ── Sequence tests ─────────────────────────────────────────────

    public static IEnumerable<SequenceTestCase> GetSequenceTestCases()
    {
        yield return new SequenceTestCase
        {
            Id = "SMUG-CLTE-CONN-CLOSE",
            Description = "CL+TE conflict — server MUST close connection after responding",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            Expected = new ExpectedBehavior
            {
                Description = "400, or 2xx + close"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Ambiguous POST (CL+TE)",
                    PayloadFactory = ctx => MakeRequest(
                        $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\nTransfer-Encoding: chunked\r\n\r\n0\r\n\r\n")
                },
                new SequenceStep
                {
                    Label = "Follow-up GET",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n")
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                // Server rejected the ambiguous request outright
                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;

                // Connection closed before or after step 1 response
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;

                // Server returned 2xx — did it close the connection?
                if (step1.Response?.StatusCode is >= 200 and < 300)
                {
                    // Connection closed after step 1 → step 2 didn't execute → correct
                    if (!step2.Executed)
                        return TestVerdict.Pass;

                    // Step 2 executed → connection was kept open → MUST-close violated
                    return TestVerdict.Fail;
                }

                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode == 400)
                    return "Rejected ambiguous CL+TE request with 400";
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed — safe";
                if (step1.Response?.StatusCode is >= 200 and < 300)
                {
                    if (!step2.Executed)
                        return $"Accepted with {step1.Response.StatusCode}, then closed connection (RFC-compliant)";
                    return $"Accepted with {step1.Response.StatusCode} but kept connection open — follow-up GET returned {step2.Response?.StatusCode.ToString() ?? "no response"}";
                }
                return $"Unexpected response: {step1.Response?.StatusCode.ToString() ?? "none"}";
            }
        };

        // ── TECL-CONN-CLOSE ─────────────────────────────────────────
        // Mirror of CLTE-CONN-CLOSE with TE listed before CL.
        // Some parsers behave differently depending on header order.
        yield return new SequenceTestCase
        {
            Id = "SMUG-TECL-CONN-CLOSE",
            Description = "TE+CL conflict (reversed order) — server MUST close connection after responding",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            Expected = new ExpectedBehavior
            {
                Description = "400, or 2xx + close"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Ambiguous POST (TE+CL)",
                    PayloadFactory = ctx => MakeRequest(
                        $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\nContent-Length: 5\r\n\r\n0\r\n\r\n")
                },
                new SequenceStep
                {
                    Label = "Follow-up GET",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n")
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;
                if (step1.Response?.StatusCode is >= 200 and < 300)
                    return !step2.Executed ? TestVerdict.Pass : TestVerdict.Fail;
                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode == 400)
                    return "Rejected ambiguous TE+CL request with 400";
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed — safe";
                if (step1.Response?.StatusCode is >= 200 and < 300)
                {
                    if (!step2.Executed)
                        return $"Accepted with {step1.Response.StatusCode}, then closed connection (RFC-compliant)";
                    return $"Accepted with {step1.Response.StatusCode} but kept connection open — follow-up GET returned {step2.Response?.StatusCode.ToString() ?? "no response"}";
                }
                return $"Unexpected response: {step1.Response?.StatusCode.ToString() ?? "none"}";
            }
        };

        // ── CLTE-DESYNC ─────────────────────────────────────────────
        // Classic CL.TE desync: CL declares a small body (6 bytes),
        // but the chunked stream includes extra data after CL's boundary.
        // If the server uses CL, the leftover bytes stay on the wire
        // and get interpreted as the start of the next request.
        //
        // Wire bytes (step 1):
        //   POST / HTTP/1.1\r\n
        //   Host: ...\r\n
        //   Content-Length: 6\r\n          ← CL says 6 bytes of body
        //   Transfer-Encoding: chunked\r\n
        //   \r\n
        //   0\r\n\r\nX                     ← TE sees terminator at byte 5; CL reads 6 bytes (includes 'X')
        //
        // If server uses TE: reads "0\r\n\r\n" (5 bytes), body done, 'X' is leftover.
        // If server uses CL: reads 6 bytes "0\r\n\r\nX", body done.
        // Either way, 'X' may poison the connection. Step 2 (GET) follows.
        // If 'X' merged with step 2's GET, the server sees "XGET / HTTP/1.1" → 400.
        // A safe server rejects step 1 with 400 or closes the connection.
        yield return new SequenceTestCase
        {
            Id = "SMUG-CLTE-DESYNC",
            Description = "CL.TE desync — leftover bytes after the body boundary may be interpreted as the next request",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            Expected = new ExpectedBehavior
            {
                Description = "400, or close"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Poison POST (CL=6, TE=chunked, extra byte)",
                    PayloadFactory = ctx => MakeRequest(
                        $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 6\r\nTransfer-Encoding: chunked\r\n\r\n0\r\n\r\nX")
                },
                new SequenceStep
                {
                    Label = "Follow-up GET",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n")
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                // Rejected the ambiguous request outright — safe
                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;

                // Connection closed — safe
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;

                if (step1.Response?.StatusCode is >= 200 and < 300)
                {
                    // Server accepted but closed connection — safe
                    if (!step2.Executed)
                        return TestVerdict.Pass;

                    // Step 2 executed. If server got a clean 2xx, it consumed our
                    // GET correctly despite the poison byte — still a MUST-close violation
                    // but not a desync. If step 2 got 400, the poison byte merged
                    // with the GET ("XGET /...") — desync detected.
                    if (step2.Response?.StatusCode == 400)
                        return TestVerdict.Fail; // Desync confirmed

                    // Connection stayed open, step 2 got 2xx — MUST-close violated
                    return TestVerdict.Fail;
                }

                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode == 400)
                    return "Rejected ambiguous CL+TE request with 400";
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed — safe";
                if (step1.Response?.StatusCode is >= 200 and < 300)
                {
                    if (!step2.Executed)
                        return $"Accepted with {step1.Response.StatusCode}, then closed connection — no desync";
                    if (step2.Response?.StatusCode == 400)
                        return $"DESYNC: Server accepted step 1 ({step1.Response.StatusCode}), but poison byte merged with follow-up GET → 400";
                    return $"Accepted with {step1.Response.StatusCode}, kept connection open — follow-up GET returned {step2.Response?.StatusCode.ToString() ?? "no response"}";
                }
                return $"Unexpected response: {step1.Response?.StatusCode.ToString() ?? "none"}";
            }
        };

        // ── CLTE-SMUGGLED-GET ────────────────────────────────────────
        // Same root cause as CLTE-DESYNC, but the "poison" is a full HTTP request.
        // If the server uses TE framing and fails to close the connection, it may execute
        // the embedded GET as a second request and send two responses back-to-back.
        yield return new SequenceTestCase
        {
            Id = "SMUG-CLTE-SMUGGLED-GET",
            Description = "CL.TE desync — embedded GET in body; multiple responses indicate request boundary confusion",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            Expected = new ExpectedBehavior
            {
                Description = "400, or close (no extra response)"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Poison POST with embedded GET",
                    PayloadFactory = ctx =>
                    {
                        var smuggled = $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n";
                        var body = "0\r\n\r\n" + smuggled;
                        var cl = Encoding.ASCII.GetByteCount(body);
                        return MakeRequest(
                            $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: {cl}\r\nTransfer-Encoding: chunked\r\n\r\n{body}");
                    }
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                // Multiple status lines in one read means the server executed a second request
                // that the client never "sent" as a separate message.
                if (statusLines >= 2)
                    return TestVerdict.Fail;

                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;

                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;

                // Server accepted an ambiguous CL+TE request and kept the connection open.
                // RFC 9112 §6.1 says it MUST close the connection after responding.
                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return $"MULTIPLE RESPONSES: observed {statusLines} HTTP status lines (embedded GET likely executed)";

                if (step1.Response?.StatusCode == 400)
                    return "Rejected ambiguous CL+TE request with 400";

                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed — safe";

                if (step1.Response?.StatusCode is >= 200 and < 300)
                    return $"Accepted with {step1.Response.StatusCode} but kept connection open — MUST-close violated (no extra response observed)";

                return $"Unexpected response: {step1.Response?.StatusCode.ToString() ?? "none"}";
            }
        };

        // ── CLTE-SMUGGLED-GET Variants (Malformed CL/TE) ───────────
        // Same as CLTE-SMUGGLED-GET, but with malformed framing headers. These are real-world vectors
        // for front-end/back-end parsing disagreements.

        yield return new SequenceTestCase
        {
            Id = "SMUG-CLTE-SMUGGLED-GET-CL-PLUS",
            Description = "CL.TE desync with malformed Content-Length (+N) — multiple responses indicate request boundary confusion",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            Expected = new ExpectedBehavior
            {
                Description = "400, or close (no extra response)"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Poison POST with embedded GET (Content-Length:+N)",
                    PayloadFactory = ctx =>
                    {
                        var smuggled = $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n";
                        var body = "0\r\n\r\n" + smuggled;
                        var cl = Encoding.ASCII.GetByteCount(body);
                        return MakeRequest(
                            $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: +{cl}\r\nTransfer-Encoding: chunked\r\n\r\n{body}");
                    }
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return TestVerdict.Fail;
                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;
                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return $"MULTIPLE RESPONSES: observed {statusLines} HTTP status lines (embedded GET likely executed)";
                if (step1.Response?.StatusCode == 400)
                    return "Rejected malformed Content-Length (+N) CL+TE request with 400";
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed — safe";
                if (step1.Response?.StatusCode is >= 200 and < 300)
                    return $"Accepted with {step1.Response.StatusCode} but kept connection open — MUST-close violated (no extra response observed)";
                return $"Unexpected response: {step1.Response?.StatusCode.ToString() ?? "none"}";
            }
        };

        yield return new SequenceTestCase
        {
            Id = "SMUG-CLTE-SMUGGLED-GET-CL-NON-NUMERIC",
            Description = "CL.TE desync with non-numeric Content-Length (N<alpha>) — multiple responses indicate request boundary confusion",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            Expected = new ExpectedBehavior
            {
                Description = "400, or close (no extra response)"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Poison POST with embedded GET (Content-Length:N<alpha>)",
                    PayloadFactory = ctx =>
                    {
                        var smuggled = $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n";
                        var body = "0\r\n\r\n" + smuggled;
                        var cl = Encoding.ASCII.GetByteCount(body);
                        return MakeRequest(
                            $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: {cl}x\r\nTransfer-Encoding: chunked\r\n\r\n{body}");
                    }
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return TestVerdict.Fail;
                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;
                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return $"MULTIPLE RESPONSES: observed {statusLines} HTTP status lines (embedded GET likely executed)";
                if (step1.Response?.StatusCode == 400)
                    return "Rejected non-numeric Content-Length (N<alpha>) CL+TE request with 400";
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed — safe";
                if (step1.Response?.StatusCode is >= 200 and < 300)
                    return $"Accepted with {step1.Response.StatusCode} but kept connection open — MUST-close violated (no extra response observed)";
                return $"Unexpected response: {step1.Response?.StatusCode.ToString() ?? "none"}";
            }
        };

        yield return new SequenceTestCase
        {
            Id = "SMUG-CLTE-SMUGGLED-GET-TE-OBS-FOLD",
            Description = "CL.TE desync with obs-folded Transfer-Encoding — multiple responses indicate request boundary confusion",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §5.2",
            Expected = new ExpectedBehavior
            {
                Description = "400, or close (no extra response)"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Poison POST with embedded GET (folded TE)",
                    PayloadFactory = ctx =>
                    {
                        var smuggled = $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n";
                        var body = "0\r\n\r\n" + smuggled;
                        var cl = Encoding.ASCII.GetByteCount(body);
                        return MakeRequest(
                            $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding:\r\n chunked\r\nContent-Length: {cl}\r\n\r\n{body}");
                    }
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return TestVerdict.Fail;
                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;
                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return $"MULTIPLE RESPONSES: observed {statusLines} HTTP status lines (embedded GET likely executed)";
                if (step1.Response?.StatusCode == 400)
                    return "Rejected folded Transfer-Encoding CL+TE request with 400";
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed — safe";
                if (step1.Response?.StatusCode is >= 200 and < 300)
                    return $"Accepted with {step1.Response.StatusCode} but kept connection open — MUST-close violated (no extra response observed)";
                return $"Unexpected response: {step1.Response?.StatusCode.ToString() ?? "none"}";
            }
        };

        // ── Request Tunneling Confirmation (HEAD) ──────────────────
        // Same as CLTE-SMUGGLED-GET, but the embedded request is HEAD.
        yield return new SequenceTestCase
        {
            Id = "SMUG-CLTE-SMUGGLED-HEAD",
            Description = "CL.TE desync — embedded HEAD in body; multiple responses indicate request boundary confusion",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            Expected = new ExpectedBehavior
            {
                Description = "400, or close (no extra response)"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Poison POST with embedded HEAD",
                    PayloadFactory = ctx =>
                    {
                        var smuggled = $"HEAD / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n";
                        var body = "0\r\n\r\n" + smuggled;
                        var cl = Encoding.ASCII.GetByteCount(body);
                        return MakeRequest(
                            $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: {cl}\r\nTransfer-Encoding: chunked\r\n\r\n{body}");
                    }
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return TestVerdict.Fail;

                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;

                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;

                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return $"MULTIPLE RESPONSES: observed {statusLines} HTTP status lines (embedded HEAD likely executed)";

                if (step1.Response?.StatusCode == 400)
                    return "Rejected ambiguous CL+TE request with 400";

                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed — safe";

                if (step1.Response?.StatusCode is >= 200 and < 300)
                    return $"Accepted with {step1.Response.StatusCode} but kept connection open — MUST-close violated (no extra response observed)";

                return $"Unexpected response: {step1.Response?.StatusCode.ToString() ?? "none"}";
            }
        };

        // ── CLTE-SMUGGLED-GET Variants (Obfuscated TE) ─────────────
        // TE parsing differentials are a common real-world smuggling vector.

        yield return new SequenceTestCase
        {
            Id = "SMUG-CLTE-SMUGGLED-GET-TE-TRAILING-SPACE",
            Description = "CL.TE desync with TE trailing space — multiple responses indicate request boundary confusion",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            Expected = new ExpectedBehavior
            {
                Description = "400, or close (no extra response)"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Poison POST with embedded GET (TE: chunked<SP>)",
                    PayloadFactory = ctx =>
                    {
                        var smuggled = $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n";
                        var body = "0\r\n\r\n" + smuggled;
                        var cl = Encoding.ASCII.GetByteCount(body);
                        return MakeRequest(
                            $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: {cl}\r\nTransfer-Encoding: chunked \r\n\r\n{body}");
                    }
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return TestVerdict.Fail;
                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;
                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return $"MULTIPLE RESPONSES: observed {statusLines} HTTP status lines (embedded GET likely executed)";
                if (step1.Response?.StatusCode == 400)
                    return "Rejected TE trailing-space CL+TE request with 400";
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed — safe";
                if (step1.Response?.StatusCode is >= 200 and < 300)
                    return $"Accepted with {step1.Response.StatusCode} but kept connection open — MUST-close violated (no extra response observed)";
                return $"Unexpected response: {step1.Response?.StatusCode.ToString() ?? "none"}";
            }
        };

        yield return new SequenceTestCase
        {
            Id = "SMUG-CLTE-SMUGGLED-GET-TE-LEADING-COMMA",
            Description = "CL.TE desync with TE leading comma — multiple responses indicate request boundary confusion",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            Expected = new ExpectedBehavior
            {
                Description = "400, or close (no extra response)"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Poison POST with embedded GET (TE: , chunked)",
                    PayloadFactory = ctx =>
                    {
                        var smuggled = $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n";
                        var body = "0\r\n\r\n" + smuggled;
                        var cl = Encoding.ASCII.GetByteCount(body);
                        return MakeRequest(
                            $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: {cl}\r\nTransfer-Encoding: , chunked\r\n\r\n{body}");
                    }
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return TestVerdict.Fail;
                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;
                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return $"MULTIPLE RESPONSES: observed {statusLines} HTTP status lines (embedded GET likely executed)";
                if (step1.Response?.StatusCode == 400)
                    return "Rejected TE leading-comma CL+TE request with 400";
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed — safe";
                if (step1.Response?.StatusCode is >= 200 and < 300)
                    return $"Accepted with {step1.Response.StatusCode} but kept connection open — MUST-close violated (no extra response observed)";
                return $"Unexpected response: {step1.Response?.StatusCode.ToString() ?? "none"}";
            }
        };

        yield return new SequenceTestCase
        {
            Id = "SMUG-CLTE-SMUGGLED-GET-TE-CASE-MISMATCH",
            Description = "CL.TE desync with TE case mismatch — multiple responses indicate request boundary confusion",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            Expected = new ExpectedBehavior
            {
                Description = "400, or close (no extra response)"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Poison POST with embedded GET (TE: Chunked)",
                    PayloadFactory = ctx =>
                    {
                        var smuggled = $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n";
                        var body = "0\r\n\r\n" + smuggled;
                        var cl = Encoding.ASCII.GetByteCount(body);
                        return MakeRequest(
                            $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: {cl}\r\nTransfer-Encoding: Chunked\r\n\r\n{body}");
                    }
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return TestVerdict.Fail;
                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;
                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return $"MULTIPLE RESPONSES: observed {statusLines} HTTP status lines (embedded GET likely executed)";
                if (step1.Response?.StatusCode == 400)
                    return "Rejected TE case-mismatch CL+TE request with 400";
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed — safe";
                if (step1.Response?.StatusCode is >= 200 and < 300)
                    return $"Accepted with {step1.Response.StatusCode} but kept connection open — MUST-close violated (no extra response observed)";
                return $"Unexpected response: {step1.Response?.StatusCode.ToString() ?? "none"}";
            }
        };

        // Duplicate/conflicting Transfer-Encoding header fields + CL, with an embedded request confirmation.
        yield return new SequenceTestCase
        {
            Id = "SMUG-TE-DUPLICATE-HEADERS-SMUGGLED-GET",
            Description = "TE.TE + CL ambiguity with embedded GET — multiple responses indicate request boundary confusion",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            Expected = new ExpectedBehavior
            {
                Description = "400, or close (no extra response)"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Poison POST with duplicate TE + embedded GET",
                    PayloadFactory = ctx =>
                    {
                        var smuggled = $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n";
                        var body = "0\r\n\r\n" + smuggled;
                        var cl = Encoding.ASCII.GetByteCount(body);
                        return MakeRequest(
                            $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\nTransfer-Encoding: identity\r\nContent-Length: {cl}\r\n\r\n{body}");
                    }
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return TestVerdict.Fail;

                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;

                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;

                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return $"MULTIPLE RESPONSES: observed {statusLines} HTTP status lines (embedded GET likely executed)";

                if (step1.Response?.StatusCode == 400)
                    return "Rejected duplicate Transfer-Encoding request with 400";

                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed — safe";

                if (step1.Response?.StatusCode is >= 200 and < 300)
                    return $"Accepted with {step1.Response.StatusCode} but kept connection open — MUST-close violated (no extra response observed)";

                return $"Unexpected response: {step1.Response?.StatusCode.ToString() ?? "none"}";
            }
        };

        // ── TECL-SMUGGLED-GET ───────────────────────────────────────
        // TE.CL smuggling confirmation: the body starts with a valid chunk-size line.
        // If the server incorrectly uses Content-Length framing, it will read only the
        // chunk-size prefix and then interpret the chunk-data (which starts with a full
        // GET request) as the next request on the connection.
        yield return new SequenceTestCase
        {
            Id = "SMUG-TECL-SMUGGLED-GET",
            Description = "TE.CL desync via chunk-size prefix trick — multiple responses indicate request boundary confusion",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            Expected = new ExpectedBehavior
            {
                Description = "400, or close (no extra response)"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Poison POST (TE=chunked, CL=prefix, chunk-data begins with GET)",
                    PayloadFactory = ctx =>
                    {
                        // Give the smuggled request a short body so that the remaining chunked framing bytes
                        // are consumed as its body if the server parses it as a second request.
                        var smuggled =
                            $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 7\r\n\r\n";

                        var chunkSize = Encoding.ASCII.GetByteCount(smuggled);
                        var hex = chunkSize.ToString("x");

                        // If a parser uses CL framing, read only the "{hex}\r\n" prefix and leave the chunk-data
                        // (which starts with the smuggled GET) on the wire as the next request.
                        var cl = hex.Length + 2;

                        var body = $"{hex}\r\n{smuggled}\r\n0\r\n\r\n";

                        return MakeRequest(
                            $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\nContent-Length: {cl}\r\n\r\n{body}");
                    }
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return TestVerdict.Fail;
                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;
                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return $"MULTIPLE RESPONSES: observed {statusLines} HTTP status lines (embedded GET likely executed)";
                if (step1.Response?.StatusCode == 400)
                    return "Rejected ambiguous TE+CL request with 400";
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed — safe";
                if (step1.Response?.StatusCode is >= 200 and < 300)
                    return $"Accepted with {step1.Response.StatusCode} but kept connection open — MUST-close violated (no extra response observed)";
                return $"Unexpected response: {step1.Response?.StatusCode.ToString() ?? "none"}";
            }
        };

        // ── DUPLICATE-CL-SMUGGLED-GET ───────────────────────────────
        // CL.CL confirmation: two different Content-Length values. If a server chooses
        // the shorter CL, the remainder begins with a valid GET request.
        yield return new SequenceTestCase
        {
            Id = "SMUG-DUPLICATE-CL-SMUGGLED-GET",
            Description = "CL.CL ambiguity with embedded GET — multiple responses indicate request boundary confusion",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §8.6",
            Expected = new ExpectedBehavior
            {
                Description = "400, or close (no extra response)"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Poison POST with duplicate Content-Length + embedded GET",
                    PayloadFactory = ctx =>
                    {
                        var smuggled = $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n";
                        var body = "PING" + smuggled;
                        var cl = Encoding.ASCII.GetByteCount(body);
                        return MakeRequest(
                            $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 4\r\nContent-Length: {cl}\r\n\r\n{body}");
                    }
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return TestVerdict.Fail;
                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;
                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var statusLines = CountHttpStatusLines(step1.Response?.RawResponse ?? "");

                if (statusLines >= 2)
                    return $"MULTIPLE RESPONSES: observed {statusLines} HTTP status lines (embedded GET likely executed)";
                if (step1.Response?.StatusCode == 400)
                    return "Rejected duplicate Content-Length request with 400";
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed — safe";
                if (step1.Response?.StatusCode is >= 200 and < 300)
                    return $"Accepted with {step1.Response.StatusCode} — duplicate Content-Length not rejected";
                return $"Unexpected response: {step1.Response?.StatusCode.ToString() ?? "none"}";
            }
        };

        // ── Ignored Content-Length / Unread-Body Desync ────────────
        // Some servers ignore request bodies on methods like GET and leave bytes on the connection.
        // This test uses an incomplete request prefix in the body and then completes it on the next write.
        yield return new SequenceTestCase
        {
            Id = "SMUG-GET-CL-PREFIX-DESYNC",
            Description = "GET with Content-Length body containing an incomplete request prefix — follow-up completes it if body was left unread",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §9.3.1",
            RfcLevel = RfcLevel.May,
            Scored = false,
            Expected = new ExpectedBehavior
            {
                Description = "400/close preferred; extra response on step 2 = warn"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "GET with CL body (request prefix, no blank line)",
                    PayloadFactory = ctx =>
                    {
                        var prefix = $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n";
                        var cl = Encoding.ASCII.GetByteCount(prefix);
                        return MakeRequest(
                            $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: {cl}\r\n\r\n{prefix}");
                    }
                },
                new SequenceStep
                {
                    Label = "Complete prefix then send follow-up GET",
                    PayloadFactory = ctx => MakeRequest(
                        $"\r\nGET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n")
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;
                if (step1.Response?.StatusCode is >= 200 and < 300)
                {
                    if (!step2.Executed)
                        return TestVerdict.Pass;

                    var statusLines2 = CountHttpStatusLines(step2.Response?.RawResponse ?? "");
                    if (statusLines2 >= 2)
                        return TestVerdict.Warn;

                    if (step2.Response?.StatusCode == 400)
                        return TestVerdict.Pass;
                    if (step2.Response?.StatusCode is >= 200 and < 300)
                        return TestVerdict.Pass;

                    return TestVerdict.Warn;
                }

                return TestVerdict.Warn;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode == 400)
                    return "Rejected GET with request-prefix body";
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed — safe";
                if (step1.Response?.StatusCode is >= 200 and < 300)
                {
                    if (!step2.Executed)
                        return $"Accepted with {step1.Response.StatusCode}, then closed connection";

                    var statusLines2 = CountHttpStatusLines(step2.Response?.RawResponse ?? "");
                    if (statusLines2 >= 2)
                        return $"MULTIPLE RESPONSES: observed {statusLines2} HTTP status lines on step 2 (unread prefix likely executed)";

                    return $"Step 1: {step1.Response.StatusCode}, step 2: {step2.Response?.StatusCode.ToString() ?? "no response"}";
                }

                return $"Unexpected response: {step1.Response?.StatusCode.ToString() ?? "none"}";
            }
        };

        // ── TECL-DESYNC ─────────────────────────────────────────────
        // Reverse desync: TE terminates early (0\r\n\r\n), but CL claims a
        // larger body. If the server uses TE, it stops at the terminator
        // and the remaining CL bytes (which include a smuggled prefix)
        // stay on the wire.
        //
        // Wire bytes (step 1):
        //   POST / HTTP/1.1\r\n
        //   Host: ...\r\n
        //   Transfer-Encoding: chunked\r\n
        //   Content-Length: 30\r\n          ← CL says 30 bytes of body
        //   \r\n
        //   0\r\n\r\nX                      ← TE ends at byte 5; CL expects 25 more
        //
        // A safe server rejects step 1 with 400 or closes the connection.
        yield return new SequenceTestCase
        {
            Id = "SMUG-TECL-DESYNC",
            Description = "TE.CL desync — chunked terminator before CL boundary, leftover bytes smuggled",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            Expected = new ExpectedBehavior
            {
                Description = "400, or close"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Poison POST (TE terminates early, CL=30)",
                    PayloadFactory = ctx => MakeRequest(
                        $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\nContent-Length: 30\r\n\r\n0\r\n\r\nX")
                },
                new SequenceStep
                {
                    Label = "Follow-up GET",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n")
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;
                if (step1.Response?.StatusCode is >= 200 and < 300)
                {
                    if (!step2.Executed)
                        return TestVerdict.Pass;
                    if (step2.Response?.StatusCode == 400)
                        return TestVerdict.Fail; // Desync confirmed
                    return TestVerdict.Fail; // MUST-close violated
                }
                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode == 400)
                    return "Rejected ambiguous TE+CL request with 400";
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed — safe";
                if (step1.Response?.StatusCode is >= 200 and < 300)
                {
                    if (!step2.Executed)
                        return $"Accepted with {step1.Response.StatusCode}, then closed connection — no desync";
                    if (step2.Response?.StatusCode == 400)
                        return $"DESYNC: Server accepted step 1 ({step1.Response.StatusCode}), but poison byte merged with follow-up GET → 400";
                    return $"Accepted with {step1.Response.StatusCode}, kept connection open — follow-up GET returned {step2.Response?.StatusCode.ToString() ?? "no response"}";
                }
                return $"Unexpected response: {step1.Response?.StatusCode.ToString() ?? "none"}";
            }
        };

        yield return new SequenceTestCase
        {
            Id = "SMUG-CL0-BODY-POISON",
            Description = "Content-Length: 0 with trailing bytes — checks if leftover bytes poison the next request",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.2",
            RfcLevel = RfcLevel.NotApplicable,
            Scored = false,
            Expected = new ExpectedBehavior
            {
                Description = "400/close preferred; poisoned follow-up = warn"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "POST with CL:0 plus poison byte",
                    PayloadFactory = ctx => MakeRequest(
                        $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 0\r\n\r\nX")
                },
                new SequenceStep
                {
                    Label = "Follow-up GET",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n")
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;
                if (step1.Response?.StatusCode is >= 200 and < 300)
                {
                    if (!step2.Executed)
                        return TestVerdict.Pass;
                    if (step2.Response?.StatusCode == 400)
                        return TestVerdict.Warn;
                    return TestVerdict.Warn;
                }
                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode == 400)
                    return "Rejected CL:0 + trailing-bytes request with 400";
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed after CL:0 request";
                if (!step2.Executed)
                    return $"Step 1 returned {step1.Response?.StatusCode}; connection then closed";
                if (step2.Response?.StatusCode == 400)
                    return $"Follow-up parsed as poisoned request (XGET...) after step 1 status {step1.Response?.StatusCode}";
                return $"Step 1: {step1.Response?.StatusCode}, step 2: {step2.Response?.StatusCode}";
            }
        };

        yield return new SequenceTestCase
        {
            Id = "SMUG-GET-CL-BODY-DESYNC",
            Description = "GET with Content-Length body followed by a second request — detects unread-body desync",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §9.3.1",
            RfcLevel = RfcLevel.May,
            Scored = false,
            Expected = new ExpectedBehavior
            {
                Description = "400/close/pass-through; poisoned follow-up = warn"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "GET with CL body",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\n\r\nhello")
                },
                new SequenceStep
                {
                    Label = "Follow-up GET",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n")
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;
                if (step1.Response?.StatusCode is >= 200 and < 300)
                {
                    if (!step2.Executed)
                        return TestVerdict.Pass;
                    if (step2.Response?.StatusCode == 400)
                        return TestVerdict.Warn;
                    if (step2.Response?.StatusCode is >= 200 and < 300)
                        return TestVerdict.Pass;
                    return TestVerdict.Warn;
                }
                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode == 400)
                    return "Rejected GET with body";
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed after GET-with-body request";
                if (!step2.Executed)
                    return $"Step 1 returned {step1.Response?.StatusCode}; connection then closed";
                if (step2.Response?.StatusCode == 400)
                    return $"Possible desync: follow-up GET returned 400 after GET-with-body acceptance ({step1.Response?.StatusCode})";
                return $"Step 1: {step1.Response?.StatusCode}, step 2: {step2.Response?.StatusCode}";
            }
        };

        yield return new SequenceTestCase
        {
            Id = "SMUG-OPTIONS-CL-BODY-DESYNC",
            Description = "OPTIONS with Content-Length body followed by a second request — detects unread-body desync",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §9.3.7",
            RfcLevel = RfcLevel.NotApplicable,
            Scored = false,
            Expected = new ExpectedBehavior
            {
                Description = "400/close/pass-through; poisoned follow-up = warn"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "OPTIONS with CL body",
                    PayloadFactory = ctx => MakeRequest(
                        $"OPTIONS / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\n\r\nhello")
                },
                new SequenceStep
                {
                    Label = "Follow-up GET",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n")
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;
                if (step1.Response?.StatusCode is >= 200 and < 300)
                {
                    if (!step2.Executed)
                        return TestVerdict.Pass;
                    if (step2.Response?.StatusCode == 400)
                        return TestVerdict.Warn;
                    if (step2.Response?.StatusCode is >= 200 and < 300)
                        return TestVerdict.Pass;
                    return TestVerdict.Warn;
                }
                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode == 400)
                    return "Rejected OPTIONS with body";
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed after OPTIONS-with-body request";
                if (!step2.Executed)
                    return $"Step 1 returned {step1.Response?.StatusCode}; connection then closed";
                if (step2.Response?.StatusCode == 400)
                    return $"Possible desync: follow-up GET returned 400 after OPTIONS-with-body acceptance ({step1.Response?.StatusCode})";
                return $"Step 1: {step1.Response?.StatusCode}, step 2: {step2.Response?.StatusCode}";
            }
        };

        yield return new SequenceTestCase
        {
            Id = "SMUG-EXPECT-100-CL-DESYNC",
            Description = "Expect: 100-continue with immediate body followed by a second request — detects unread-body desync",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §10.1.1",
            RfcLevel = RfcLevel.NotApplicable,
            Scored = false,
            Expected = new ExpectedBehavior
            {
                Description = "417/400/close preferred; poisoned follow-up = warn"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "POST with Expect: 100-continue and body",
                    PayloadFactory = ctx => MakeRequest(
                        $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\nExpect: 100-continue\r\n\r\nhello")
                },
                new SequenceStep
                {
                    Label = "Follow-up GET",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n")
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode is 400 or 417)
                    return TestVerdict.Pass;
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;
                if (step1.Response?.StatusCode is >= 200 and < 300)
                {
                    if (!step2.Executed)
                        return TestVerdict.Pass;
                    if (step2.Response?.StatusCode == 400)
                        return TestVerdict.Warn;
                    if (step2.Response?.StatusCode is >= 200 and < 300)
                        return TestVerdict.Pass;
                    return TestVerdict.Warn;
                }
                return TestVerdict.Warn;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode is 400 or 417)
                    return $"Rejected Expect workflow with {step1.Response.StatusCode}";
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed after Expect request";
                if (!step2.Executed)
                    return $"Step 1 returned {step1.Response?.StatusCode}; connection then closed";
                if (step2.Response?.StatusCode == 400)
                    return $"Possible desync: follow-up GET returned 400 after Expect acceptance ({step1.Response?.StatusCode})";
                return $"Step 1: {step1.Response?.StatusCode}, step 2: {step2.Response?.StatusCode}";
            }
        };

        yield return new SequenceTestCase
        {
            Id = "SMUG-OPTIONS-TE-OBS-FOLD",
            Description = "OPTIONS with TE obs-fold and CL present — server must reject or close after response",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §5.2",
            Expected = new ExpectedBehavior
            {
                Description = "400, or 2xx + close"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "OPTIONS with folded TE and CL",
                    PayloadFactory = ctx => MakeRequest(
                        $"OPTIONS / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding:\r\n chunked\r\nContent-Length: 5\r\n\r\nhello")
                },
                new SequenceStep
                {
                    Label = "Follow-up GET",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n")
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;
                if (step1.Response?.StatusCode is >= 200 and < 300)
                    return !step2.Executed ? TestVerdict.Pass : TestVerdict.Fail;
                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode == 400)
                    return "Rejected folded Transfer-Encoding request with 400";
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed after OPTIONS folded-TE request";
                if (step1.Response?.StatusCode is >= 200 and < 300)
                {
                    if (!step2.Executed)
                        return $"Accepted with {step1.Response.StatusCode}, then closed connection";
                    return $"Accepted with {step1.Response.StatusCode} but kept connection open — follow-up GET returned {step2.Response?.StatusCode.ToString() ?? "no response"}";
                }
                return $"Unexpected response: {step1.Response?.StatusCode.ToString() ?? "none"}";
            }
        };

        yield return new SequenceTestCase
        {
            Id = "SMUG-CHUNK-INVALID-SIZE-DESYNC",
            Description = "Invalid chunk size (+0) with poison byte — detects chunk-size parser desync",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §7.1",
            Expected = new ExpectedBehavior
            {
                Description = "400, or close"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Poison POST with invalid chunk-size (+0)",
                    PayloadFactory = ctx => MakeRequest(
                        $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n+0\r\n\r\nX")
                },
                new SequenceStep
                {
                    Label = "Follow-up GET",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n")
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode == 400)
                    return TestVerdict.Pass;
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return TestVerdict.Pass;
                if (step1.Response?.StatusCode is >= 200 and < 300)
                {
                    if (!step2.Executed)
                        return TestVerdict.Pass;
                    if (step2.Response?.StatusCode == 400)
                        return TestVerdict.Fail;
                    return TestVerdict.Fail;
                }
                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode == 400)
                    return "Rejected invalid chunk-size request with 400";
                if (!step1.Executed || step1.ConnectionState == ConnectionState.ClosedByServer)
                    return "Connection closed after invalid chunk-size request";
                if (step1.Response?.StatusCode is >= 200 and < 300)
                {
                    if (!step2.Executed)
                        return $"Accepted with {step1.Response.StatusCode}, then closed connection";
                    if (step2.Response?.StatusCode == 400)
                        return $"DESYNC: accepted invalid chunk-size request ({step1.Response.StatusCode}), follow-up GET parsed as poisoned request";
                    return $"Accepted invalid chunk-size request and kept connection open — follow-up GET returned {step2.Response?.StatusCode.ToString() ?? "no response"}";
                }
                return $"Unexpected response: {step1.Response?.StatusCode.ToString() ?? "none"}";
            }
        };

        // ── PIPELINE-SAFE ───────────────────────────────────────────
        // Baseline: two clean, unambiguous GET requests on one connection.
        // Validates that the server supports normal HTTP/1.1 pipelining.
        // If this fails, all other sequence tests are unreliable.
        yield return new SequenceTestCase
        {
            Id = "SMUG-PIPELINE-SAFE",
            Description = "Baseline — two clean GET requests on one keep-alive connection",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §9.3",
            Scored = false,
            RfcLevel = RfcLevel.Should,
            Expected = new ExpectedBehavior
            {
                Description = "2xx + 2xx"
            },
            Steps =
            [
                new SequenceStep
                {
                    Label = "First GET",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n")
                },
                new SequenceStep
                {
                    Label = "Second GET",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\n\r\n")
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                // Step 1 must succeed
                if (step1.Response?.StatusCode is not (>= 200 and < 300))
                    return TestVerdict.Fail;

                // Connection closed after step 1 — server doesn't support pipelining
                if (!step2.Executed)
                    return TestVerdict.Warn;

                // Both steps got 2xx — pipelining works
                if (step2.Response?.StatusCode is >= 200 and < 300)
                    return TestVerdict.Pass;

                return TestVerdict.Warn;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (step1.Response?.StatusCode is not (>= 200 and < 300))
                    return $"First GET failed with {step1.Response?.StatusCode.ToString() ?? "no response"}";
                if (!step2.Executed)
                    return $"First GET returned {step1.Response.StatusCode}, but server closed connection — no pipelining support";
                if (step2.Response?.StatusCode is >= 200 and < 300)
                    return $"Both GETs returned {step1.Response.StatusCode}/{step2.Response.StatusCode} — pipelining works";
                return $"First GET: {step1.Response.StatusCode}, second GET: {step2.Response?.StatusCode.ToString() ?? "no response"}";
            }
        };
    }

    private static byte[] MakeRequest(string request) => Encoding.ASCII.GetBytes(request);
}
