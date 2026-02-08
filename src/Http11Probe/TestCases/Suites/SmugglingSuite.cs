using System.Text;
using Http11Probe.Client;

namespace Http11Probe.TestCases.Suites;

public static class SmugglingSuite
{
    public static IEnumerable<TestCase> GetTestCases()
    {
        yield return new TestCase
        {
            Id = "SMUG-CL-TE-BOTH",
            Description = "Both Content-Length and Transfer-Encoding present must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 6\r\nTransfer-Encoding: chunked\r\n\r\n0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-DUPLICATE-CL",
            Description = "Duplicate Content-Length with different values must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §8.6",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\nContent-Length: 10\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CL-LEADING-ZEROS",
            Description = "Content-Length with leading zeros should be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §8.6",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 005\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
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
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
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
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
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
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // 2xx is RFC-compliant (OWS trimming) but worth noting
                    return TestVerdict.Warn;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-HEADER-INJECTION",
            Description = "Apparent CRLF injection — payload is actually two valid headers on the wire",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §5.5",
            PayloadFactory = ctx => MakeRequest(
                $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nX-Test: val\r\nInjected: yes\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // 2xx is correct — these are two well-formed headers
                    return TestVerdict.Warn;
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
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    return TestVerdict.Warn;
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
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // Extra OWS is technically valid per RFC
                    return TestVerdict.Warn;
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
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // Case-insensitive matching is valid per RFC
                    return TestVerdict.Warn;
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
            Description = "Transfer-Encoding where chunked is not the final encoding must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §7",
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
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-TE-LEADING-COMMA",
            Description = "Transfer-Encoding with leading comma (, chunked) must be rejected",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §6.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: , chunked\r\nContent-Length: 5\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
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
            Description = "Bare LF in chunk extension must be rejected (TERM.EXT vector)",
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
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
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
            Description = "Bare LF as chunk data terminator must be rejected",
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
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
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
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CHUNK-LF-TRAILER",
            Description = "Bare LF in chunked trailer section termination must be rejected",
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
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
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
            Expected = new ExpectedBehavior
            {
                ExpectedStatus = StatusCodeRange.Exact(400),
                AllowConnectionClose = true
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
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer_Encoding: chunked\r\nContent-Length: 5\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // 2xx is valid — underscore makes it a different header name
                    return TestVerdict.Warn;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CL-COMMA-SAME",
            Description = "Content-Length with comma-separated identical values — some servers merge",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §8.6",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5, 5\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // 2xx is tolerable — RFC allows merging identical CL values
                    return TestVerdict.Warn;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-CHUNKED-WITH-PARAMS",
            Description = "Transfer-Encoding: chunked;ext=val — parameters on chunked encoding",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9112 §7",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked;ext=val\r\nContent-Length: 5\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    return TestVerdict.Warn;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-EXPECT-100-CL",
            Description = "Expect: 100-continue with Content-Length — server should send 100 then read body",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §10.1.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\nExpect: 100-continue\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // 2xx or 100 means server handled Expect properly
                    return TestVerdict.Warn;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-TRAILER-CL",
            Description = "Content-Length in chunked trailers must be ignored — prohibited trailer field",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §6.5.1",
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5\r\nhello\r\n0\r\nContent-Length: 50\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
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
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5\r\nhello\r\n0\r\nTransfer-Encoding: chunked\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
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
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5\r\nhello\r\n0\r\nHost: evil.example.com\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
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
            PayloadFactory = ctx => MakeRequest(
                $"POST / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer-Encoding: chunked\r\n\r\n5\r\nhello\r\n0\r\nAuthorization: Bearer evil\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
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
            PayloadFactory = ctx => MakeRequest(
                $"HEAD / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    // Server responded to HEAD — check that it consumed or closed
                    return TestVerdict.Warn;
                }
            }
        };

        yield return new TestCase
        {
            Id = "SMUG-OPTIONS-CL-BODY",
            Description = "OPTIONS with Content-Length and body — server should consume or reject body",
            Category = TestCategory.Smuggling,
            RfcReference = "RFC 9110 §9.3.7",
            PayloadFactory = ctx => MakeRequest(
                $"OPTIONS / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 5\r\n\r\nhello"),
            Expected = new ExpectedBehavior
            {
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass;
                    return TestVerdict.Warn;
                }
            }
        };
    }

    private static byte[] MakeRequest(string request) => Encoding.ASCII.GetBytes(request);
}
