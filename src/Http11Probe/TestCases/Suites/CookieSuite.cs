using System.Text;
using Http11Probe.Client;
using Http11Probe.Response;

namespace Http11Probe.TestCases.Suites;

public static class CookieSuite
{
    public static IEnumerable<TestCase> GetTestCases()
    {
        // ── Echo-based tests (target /echo, all servers) ─────────────

        yield return new TestCase
        {
            Id = "COOK-ECHO",
            Description = "Basic Cookie header echoed back by /echo endpoint",
            Category = TestCategory.Cookies,
            Scored = false,
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = ctx => MakeRequest(
                $"GET /echo HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nCookie: foo=bar\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx with Cookie in body",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Warn : TestVerdict.Fail;
                    if (response.StatusCode is < 200 or >= 300)
                        return TestVerdict.Fail;
                    var body = response.Body ?? "";
                    return body.Contains("Cookie:", StringComparison.OrdinalIgnoreCase)
                        ? TestVerdict.Pass
                        : TestVerdict.Fail;
                }
            },
            BehavioralAnalyzer = response =>
            {
                if (response is null) return null;
                var body = response.Body ?? "";
                if (body.Contains("foo=bar")) return "Cookie echoed: foo=bar";
                if (body.Contains("Cookie:", StringComparison.OrdinalIgnoreCase)) return "Cookie header present but value differs";
                return "Cookie header missing from echo";
            }
        };

        yield return new TestCase
        {
            Id = "COOK-OVERSIZED",
            Description = "64KB Cookie header — tests header size limits on cookie data",
            Category = TestCategory.Cookies,
            Scored = false,
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = ctx =>
            {
                var bigValue = new string('A', 65_536);
                return MakeRequest(
                    $"GET /echo HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nCookie: big={bigValue}\r\n\r\n");
            },
            Expected = new ExpectedBehavior
            {
                Description = "400/431 (rejected) or 2xx (survived)",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode is 400 or 431)
                        return TestVerdict.Pass;
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Pass; // survived
                    return TestVerdict.Fail; // 500 = crash
                }
            },
            BehavioralAnalyzer = response =>
            {
                if (response is null) return null;
                if (response.StatusCode is 400 or 431) return "Rejected oversized cookie";
                if (response.StatusCode is >= 200 and < 300) return "Accepted 64KB cookie";
                return $"Unexpected: {response.StatusCode}";
            }
        };

        yield return new TestCase
        {
            Id = "COOK-EMPTY",
            Description = "Empty Cookie header value — tests parser resilience",
            Category = TestCategory.Cookies,
            Scored = false,
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = ctx => MakeRequest(
                $"GET /echo HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nCookie: \r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx or 400",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode is (>= 200 and < 300) or 400)
                        return TestVerdict.Pass;
                    return TestVerdict.Fail; // 500 = crash
                }
            },
            BehavioralAnalyzer = response =>
            {
                if (response is null) return null;
                if (response.StatusCode is >= 200 and < 300) return "Accepted empty cookie";
                if (response.StatusCode == 400) return "Rejected empty cookie";
                return $"Unexpected: {response.StatusCode}";
            }
        };

        yield return new TestCase
        {
            Id = "COOK-NUL",
            Description = "NUL byte in cookie value — dangerous if preserved by parser",
            Category = TestCategory.Cookies,
            Scored = false,
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = ctx =>
            {
                var request = $"GET /echo HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nCookie: foo=\0bar\r\n\r\n";
                return Encoding.ASCII.GetBytes(request);
            },
            Expected = new ExpectedBehavior
            {
                Description = "400 (rejected) or 2xx without NUL",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass; // rejected
                    if (response.StatusCode is >= 200 and < 300)
                    {
                        var body = response.Body ?? "";
                        // NUL preserved in output = dangerous
                        return body.Contains('\0') ? TestVerdict.Fail : TestVerdict.Pass;
                    }
                    return TestVerdict.Fail; // 500 = crash
                }
            },
            BehavioralAnalyzer = response =>
            {
                if (response is null) return null;
                if (response.StatusCode == 400) return "Rejected NUL in cookie";
                if (response.StatusCode is >= 200 and < 300)
                {
                    var body = response.Body ?? "";
                    return body.Contains('\0') ? "NUL byte preserved (dangerous)" : "NUL stripped or cookie dropped";
                }
                return $"Unexpected: {response.StatusCode}";
            }
        };

        yield return new TestCase
        {
            Id = "COOK-CONTROL-CHARS",
            Description = "Control characters (0x01-0x03) in cookie value — dangerous if preserved",
            Category = TestCategory.Cookies,
            Scored = false,
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = ctx =>
            {
                var request = $"GET /echo HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nCookie: foo=\x01\x02\x03\r\n\r\n";
                return Encoding.ASCII.GetBytes(request);
            },
            Expected = new ExpectedBehavior
            {
                Description = "400 (rejected) or 2xx without control chars",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass; // rejected
                    if (response.StatusCode is >= 200 and < 300)
                    {
                        var body = response.Body ?? "";
                        // Control chars preserved = dangerous
                        return body.Any(c => c is '\x01' or '\x02' or '\x03')
                            ? TestVerdict.Fail
                            : TestVerdict.Pass;
                    }
                    return TestVerdict.Fail; // 500 = crash
                }
            },
            BehavioralAnalyzer = response =>
            {
                if (response is null) return null;
                if (response.StatusCode == 400) return "Rejected control chars in cookie";
                if (response.StatusCode is >= 200 and < 300)
                {
                    var body = response.Body ?? "";
                    return body.Any(c => c is '\x01' or '\x02' or '\x03')
                        ? "Control chars preserved (dangerous)"
                        : "Control chars stripped or cookie dropped";
                }
                return $"Unexpected: {response.StatusCode}";
            }
        };

        yield return new TestCase
        {
            Id = "COOK-MANY-PAIRS",
            Description = "1000 cookie key=value pairs — tests parser performance limits",
            Category = TestCategory.Cookies,
            Scored = false,
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = ctx =>
            {
                var pairs = string.Join("; ", Enumerable.Range(0, 1000).Select(i => $"k{i}=v{i}"));
                return MakeRequest(
                    $"GET /echo HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nCookie: {pairs}\r\n\r\n");
            },
            Expected = new ExpectedBehavior
            {
                Description = "2xx or 400/431",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode is (>= 200 and < 300) or 400 or 431)
                        return TestVerdict.Pass;
                    return TestVerdict.Fail; // 500 = crash
                }
            },
            BehavioralAnalyzer = response =>
            {
                if (response is null) return null;
                if (response.StatusCode is >= 200 and < 300) return "Accepted 1000 cookie pairs";
                if (response.StatusCode is 400 or 431) return "Rejected 1000 cookie pairs";
                return $"Unexpected: {response.StatusCode}";
            }
        };

        yield return new TestCase
        {
            Id = "COOK-MALFORMED",
            Description = "Completely malformed cookie value (===;;;) — tests parser crash resilience",
            Category = TestCategory.Cookies,
            Scored = false,
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = ctx => MakeRequest(
                $"GET /echo HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nCookie: ===;;;\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx or 400",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
                    if (response.StatusCode is (>= 200 and < 300) or 400)
                        return TestVerdict.Pass;
                    return TestVerdict.Fail; // 500 = crash
                }
            },
            BehavioralAnalyzer = response =>
            {
                if (response is null) return null;
                if (response.StatusCode is >= 200 and < 300) return "Accepted malformed cookie";
                if (response.StatusCode == 400) return "Rejected malformed cookie";
                return $"Unexpected: {response.StatusCode}";
            }
        };

        yield return new TestCase
        {
            Id = "COOK-MULTI-HEADER",
            Description = "Two separate Cookie headers — should be folded per RFC 6265 §5.4",
            Category = TestCategory.Cookies,
            Scored = false,
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = ctx => MakeRequest(
                $"GET /echo HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nCookie: a=1\r\nCookie: b=2\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx with both cookies",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Warn : TestVerdict.Fail;
                    if (response.StatusCode is >= 200 and < 300)
                    {
                        var body = response.Body ?? "";
                        var hasA = body.Contains("a=1");
                        var hasB = body.Contains("b=2");
                        return hasA && hasB ? TestVerdict.Pass : TestVerdict.Warn;
                    }
                    if (response.StatusCode == 400)
                        return TestVerdict.Warn; // Rejected but didn't crash
                    return TestVerdict.Fail; // 500 = crash
                }
            },
            BehavioralAnalyzer = response =>
            {
                if (response is null) return null;
                if (response.StatusCode is >= 200 and < 300)
                {
                    var body = response.Body ?? "";
                    var hasA = body.Contains("a=1");
                    var hasB = body.Contains("b=2");
                    if (hasA && hasB) return "Both cookies echoed";
                    if (hasA || hasB) return "Only one cookie echoed";
                    return "Neither cookie echoed";
                }
                if (response.StatusCode == 400) return "Rejected multiple Cookie headers";
                return $"Unexpected: {response.StatusCode}";
            }
        };

        // ── Parsed-cookie tests (target /cookie, AspNetMinimal only) ─

        yield return new TestCase
        {
            Id = "COOK-PARSED-BASIC",
            Description = "Basic cookie parsed correctly by framework",
            Category = TestCategory.Cookies,
            Scored = false,
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = ctx => MakeRequest(
                $"GET /cookie HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nCookie: foo=bar\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx with foo=bar in body",
                CustomValidator = MakeParsedValidator("foo=bar")
            },
            BehavioralAnalyzer = MakeParsedAnalyzer("foo=bar")
        };

        yield return new TestCase
        {
            Id = "COOK-PARSED-MULTI",
            Description = "Multiple cookies parsed correctly by framework",
            Category = TestCategory.Cookies,
            Scored = false,
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = ctx => MakeRequest(
                $"GET /cookie HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nCookie: a=1; b=2; c=3\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx with a=1, b=2, c=3 in body",
                CustomValidator = MakeParsedValidator("a=1", "b=2", "c=3")
            },
            BehavioralAnalyzer = MakeParsedAnalyzer("a=1", "b=2", "c=3")
        };

        yield return new TestCase
        {
            Id = "COOK-PARSED-EMPTY-VAL",
            Description = "Cookie with empty value parsed without crash",
            Category = TestCategory.Cookies,
            Scored = false,
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = ctx => MakeRequest(
                $"GET /cookie HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nCookie: foo=\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx (no crash)",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Warn : TestVerdict.Fail;
                    if (response.StatusCode == 404)
                        return TestVerdict.Warn; // endpoint not available
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Pass;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass; // rejected but didn't crash
                    return TestVerdict.Fail; // 500 = crash
                }
            },
            BehavioralAnalyzer = response =>
            {
                if (response is null) return null;
                if (response.StatusCode == 404) return "Endpoint not available";
                if (response.StatusCode is >= 200 and < 300)
                {
                    var body = response.Body ?? "";
                    return body.Contains("foo=") ? "Parsed foo= (empty value)" : "Survived (cookie may have been dropped)";
                }
                if (response.StatusCode == 400) return "Rejected empty cookie value";
                return $"Unexpected: {response.StatusCode}";
            }
        };

        yield return new TestCase
        {
            Id = "COOK-PARSED-SPECIAL",
            Description = "Cookies with spaces and = in values — tests framework parser edge cases",
            Category = TestCategory.Cookies,
            Scored = false,
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = ctx => MakeRequest(
                $"GET /cookie HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nCookie: a=hello world; b=x=y\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "2xx (no crash)",
                CustomValidator = (response, state) =>
                {
                    if (response is null)
                        return state == ConnectionState.ClosedByServer ? TestVerdict.Warn : TestVerdict.Fail;
                    if (response.StatusCode == 404)
                        return TestVerdict.Warn; // endpoint not available
                    if (response.StatusCode is >= 200 and < 300)
                        return TestVerdict.Pass;
                    if (response.StatusCode == 400)
                        return TestVerdict.Pass; // rejected but didn't crash
                    return TestVerdict.Fail; // 500 = crash
                }
            },
            BehavioralAnalyzer = response =>
            {
                if (response is null) return null;
                if (response.StatusCode == 404) return "Endpoint not available";
                if (response.StatusCode is >= 200 and < 300)
                {
                    var body = response.Body ?? "";
                    var hasA = body.Contains("a=");
                    var hasB = body.Contains("b=");
                    if (hasA && hasB) return "Both cookies parsed";
                    if (hasA || hasB) return "Partially parsed";
                    return "Survived but no cookies parsed";
                }
                if (response.StatusCode == 400) return "Rejected special characters in cookie";
                return $"Unexpected: {response.StatusCode}";
            }
        };
    }

    private static Func<HttpResponse?, ConnectionState, TestVerdict> MakeParsedValidator(
        params string[] expectedPairs)
    {
        return (response, state) =>
        {
            if (response is null)
                return state == ConnectionState.ClosedByServer ? TestVerdict.Warn : TestVerdict.Fail;
            if (response.StatusCode == 404)
                return TestVerdict.Warn; // endpoint not available on this server
            if (response.StatusCode is >= 200 and < 300)
            {
                var body = response.Body ?? "";
                return expectedPairs.All(p => body.Contains(p))
                    ? TestVerdict.Pass
                    : TestVerdict.Fail;
            }
            if (response.StatusCode == 400)
                return TestVerdict.Warn;
            return TestVerdict.Fail; // 500 = crash
        };
    }

    private static Func<HttpResponse?, string?> MakeParsedAnalyzer(params string[] expectedPairs)
    {
        return response =>
        {
            if (response is null) return null;
            if (response.StatusCode == 404) return "Endpoint not available";
            if (response.StatusCode is >= 200 and < 300)
            {
                var body = response.Body ?? "";
                var found = expectedPairs.Count(p => body.Contains(p));
                return found == expectedPairs.Length
                    ? $"All {found} cookie(s) parsed"
                    : $"{found}/{expectedPairs.Length} cookie(s) found";
            }
            if (response.StatusCode == 400) return "Rejected";
            return $"Unexpected: {response.StatusCode}";
        };
    }

    private static byte[] MakeRequest(string request) => Encoding.ASCII.GetBytes(request);
}
