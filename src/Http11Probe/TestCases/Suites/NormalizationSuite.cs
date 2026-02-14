using System.Text;
using Http11Probe.Client;
using Http11Probe.Response;

namespace Http11Probe.TestCases.Suites;

public static class NormalizationSuite
{
    private static Dictionary<string, List<string>> ParseEchoHeaders(string body)
    {
        var result = new Dictionary<string, List<string>>();
        foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var colonIdx = line.IndexOf(':');
            if (colonIdx <= 0) continue;
            var name = line[..colonIdx];
            var value = line[(colonIdx + 1)..].TrimStart().TrimEnd('\r');
            if (!result.TryGetValue(name, out var list))
            {
                list = new List<string>();
                result[name] = list;
            }
            list.Add(value);
        }
        return result;
    }

    /// <summary>
    /// Scans echo headers for the probe value and determines if the header name
    /// was normalized (mapped to standard form) or preserved (kept as sent).
    /// </summary>
    private static (bool normalized, bool preserved, string? displayName) CheckEchoHeaders(
        string body, string standardName, string malformedName, string probeValue)
    {
        if (string.IsNullOrWhiteSpace(body) || !body.Contains(':'))
            return (false, false, null);

        var headers = ParseEchoHeaders(body);

        // First pass: look for normalized form (standard name with probe value)
        foreach (var (name, values) in headers)
        {
            if (!values.Any(v => v.Equals(probeValue, StringComparison.OrdinalIgnoreCase)))
                continue;
            // Exact match with standard name → clearly normalized
            if (name.Equals(standardName, StringComparison.Ordinal) &&
                !name.Equals(malformedName, StringComparison.Ordinal))
                return (true, false, name);
            // Case-insensitive match (e.g. Node.js lowercasing) → still normalized
            if (name.Equals(standardName, StringComparison.OrdinalIgnoreCase) &&
                !name.Equals(malformedName, StringComparison.OrdinalIgnoreCase))
                return (true, false, name);
        }

        // Second pass: look for preserved form (malformed name with probe value)
        foreach (var (name, values) in headers)
        {
            if (!values.Any(v => v.Equals(probeValue, StringComparison.OrdinalIgnoreCase)))
                continue;
            if (name.Equals(malformedName, StringComparison.OrdinalIgnoreCase))
                return (false, true, name);
        }

        return (false, false, null);
    }

    private static Func<HttpResponse?, ConnectionState, TestVerdict> MakeValidator(
        string standardName, string malformedName, string probeValue)
    {
        return (response, state) =>
        {
            if (response is null)
                return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
            if (response.StatusCode >= 400)
                return TestVerdict.Pass;
            if (response.StatusCode is >= 200 and < 300)
            {
                var (normalized, preserved, _) = CheckEchoHeaders(
                    response.Body ?? "", standardName, malformedName, probeValue);
                if (normalized) return TestVerdict.Fail;
                if (preserved) return TestVerdict.Warn;
                return TestVerdict.Pass;
            }
            return TestVerdict.Fail;
        };
    }

    private static Func<HttpResponse?, string?> MakeAnalyzer(
        string standardName, string malformedName, string probeValue)
    {
        return response =>
        {
            if (response is null || response.StatusCode is < 200 or >= 300) return null;
            var body = response.Body ?? "";
            if (string.IsNullOrWhiteSpace(body) || !body.Contains(':'))
                return "Static response";
            var (normalized, preserved, displayName) = CheckEchoHeaders(
                body, standardName, malformedName, probeValue);
            if (normalized) return $"Normalized: {malformedName} \u2192 {displayName}";
            if (preserved) return $"Preserved: {displayName}";
            return "Dropped";
        };
    }

    /// <summary>
    /// Validator for NORM-CASE-TE where standard and malformed names differ only
    /// in casing. Uses case-sensitive matching to distinguish normalization from preservation.
    /// </summary>
    private static Func<HttpResponse?, ConnectionState, TestVerdict> MakeCaseValidator(
        string standardName, string originalCasing, string probeValue)
    {
        return (response, state) =>
        {
            if (response is null)
                return state == ConnectionState.ClosedByServer ? TestVerdict.Pass : TestVerdict.Fail;
            if (response.StatusCode >= 400)
                return TestVerdict.Pass;
            if (response.StatusCode is >= 200 and < 300)
            {
                var body = response.Body ?? "";
                if (string.IsNullOrWhiteSpace(body) || !body.Contains(':'))
                    return TestVerdict.Pass;
                var headers = ParseEchoHeaders(body);
                foreach (var (name, values) in headers)
                {
                    if (!values.Any(v => v.Equals(probeValue, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    // Exact match with original casing → preserved
                    if (name == originalCasing) return TestVerdict.Warn;
                    // Any other casing → normalized
                    if (name.Equals(standardName, StringComparison.OrdinalIgnoreCase))
                        return TestVerdict.Fail;
                }
                return TestVerdict.Pass;
            }
            return TestVerdict.Fail;
        };
    }

    private static Func<HttpResponse?, string?> MakeCaseAnalyzer(
        string standardName, string originalCasing, string probeValue)
    {
        return response =>
        {
            if (response is null || response.StatusCode is < 200 or >= 300) return null;
            var body = response.Body ?? "";
            if (string.IsNullOrWhiteSpace(body) || !body.Contains(':'))
                return "Static response";
            var headers = ParseEchoHeaders(body);
            foreach (var (name, values) in headers)
            {
                if (!values.Any(v => v.Equals(probeValue, StringComparison.OrdinalIgnoreCase)))
                    continue;
                if (name == originalCasing) return $"Preserved: {name}";
                if (name.Equals(standardName, StringComparison.OrdinalIgnoreCase))
                    return $"Normalized: {originalCasing} \u2192 {name}";
            }
            return "Dropped";
        };
    }

    public static IEnumerable<TestCase> GetTestCases()
    {
        yield return new TestCase
        {
            Id = "NORM-UNDERSCORE-CL",
            Description = "Underscore in Content-Length name — checks if server normalizes Content_Length to Content-Length",
            Category = TestCategory.Normalization,
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = ctx => MakeRequest(
                $"POST /echo HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 11\r\nContent_Length: 99\r\n\r\nhello world"),
            Expected = new ExpectedBehavior
            {
                Description = "Reject/drop (pass), normalize (fail), preserve (warn)",
                CustomValidator = MakeValidator("Content-Length", "Content_Length", "99")
            },
            BehavioralAnalyzer = MakeAnalyzer("Content-Length", "Content_Length", "99")
        };

        yield return new TestCase
        {
            Id = "NORM-SP-BEFORE-COLON-CL",
            Description = "Space before colon in Content-Length — checks if server strips whitespace before colon",
            Category = TestCategory.Normalization,
            RfcReference = "RFC 9112 §5",
            PayloadFactory = ctx => MakeRequest(
                $"POST /echo HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 11\r\nContent-Length : 5\r\n\r\nhello world"),
            Expected = new ExpectedBehavior
            {
                Description = "Reject/drop (pass), normalize (fail), preserve (warn)",
                CustomValidator = MakeValidator("Content-Length", "Content-Length ", "5")
            },
            BehavioralAnalyzer = MakeAnalyzer("Content-Length", "Content-Length ", "5")
        };

        yield return new TestCase
        {
            Id = "NORM-TAB-IN-NAME",
            Description = "Tab character in header name — checks if server normalizes Content\\tLength to Content-Length",
            Category = TestCategory.Normalization,
            PayloadFactory = ctx => MakeRequest(
                $"POST /echo HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nContent-Length: 11\r\nContent\tLength: 99\r\n\r\nhello world"),
            Expected = new ExpectedBehavior
            {
                Description = "Reject/drop (pass), normalize (fail), preserve (warn)",
                CustomValidator = MakeValidator("Content-Length", "Content\tLength", "99")
            },
            BehavioralAnalyzer = MakeAnalyzer("Content-Length", "Content\tLength", "99")
        };

        yield return new TestCase
        {
            Id = "NORM-CASE-TE",
            Description = "All-uppercase TRANSFER-ENCODING — checks if server normalizes header name casing",
            Category = TestCategory.Normalization,
            RfcLevel = RfcLevel.NotApplicable,
            Scored = false,
            PayloadFactory = ctx => MakeRequest(
                $"POST /echo HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTRANSFER-ENCODING: chunked\r\n\r\nB\r\nhello world\r\n0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "Reject/drop (pass), normalize casing (fail), preserve (warn)",
                CustomValidator = MakeCaseValidator("Transfer-Encoding", "TRANSFER-ENCODING", "chunked")
            },
            BehavioralAnalyzer = MakeCaseAnalyzer("Transfer-Encoding", "TRANSFER-ENCODING", "chunked")
        };

        yield return new TestCase
        {
            Id = "NORM-UNDERSCORE-TE",
            Description = "Underscore in Transfer-Encoding name — checks if server normalizes Transfer_Encoding to Transfer-Encoding",
            Category = TestCategory.Normalization,
            RfcLevel = RfcLevel.NotApplicable,
            PayloadFactory = ctx => MakeRequest(
                $"POST /echo HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nTransfer_Encoding: chunked\r\n\r\nB\r\nhello world\r\n0\r\n\r\n"),
            Expected = new ExpectedBehavior
            {
                Description = "Reject/drop (pass), normalize (fail), preserve (warn)",
                CustomValidator = MakeValidator("Transfer-Encoding", "Transfer_Encoding", "chunked")
            },
            BehavioralAnalyzer = MakeAnalyzer("Transfer-Encoding", "Transfer_Encoding", "chunked")
        };
    }

    private static byte[] MakeRequest(string request) => Encoding.ASCII.GetBytes(request);
}
