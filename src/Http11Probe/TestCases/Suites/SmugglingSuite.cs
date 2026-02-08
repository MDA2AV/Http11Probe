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
    }

    private static byte[] MakeRequest(string request) => Encoding.ASCII.GetBytes(request);
}
