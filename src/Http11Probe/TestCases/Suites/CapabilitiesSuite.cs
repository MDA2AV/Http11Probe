using System.Text;
using Http11Probe.Client;

namespace Http11Probe.TestCases.Suites;

public static class CapabilitiesSuite
{
    private static byte[] MakeRequest(string raw) => Encoding.ASCII.GetBytes(raw);

    private static string? GetHeader(StepResult step, string headerName)
    {
        if (step.Response?.Headers is null) return null;
        foreach (var kv in step.Response.Headers)
        {
            if (string.Equals(kv.Key, headerName, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        }
        return null;
    }

    public static IEnumerable<SequenceTestCase> GetSequenceTestCases()
    {
        // ── CAP-ETAG-304 ──────────────────────────────────────────────
        yield return new SequenceTestCase
        {
            Id = "CAP-ETAG-304",
            Description = "ETag conditional GET returns 304 Not Modified",
            Category = TestCategory.Capabilities,
            Scored = false,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9110 §13.1.2",
            Expected = new ExpectedBehavior { Description = "304" },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Initial GET (capture ETag)",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nConnection: keep-alive\r\n\r\n")
                },
                new SequenceStep
                {
                    Label = "Conditional GET (If-None-Match)",
                    DynamicPayloadFactory = (ctx, previousSteps) =>
                    {
                        var etag = GetHeader(previousSteps[0], "ETag") ?? "\"no-etag\"";
                        return MakeRequest(
                            $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nIf-None-Match: {etag}\r\n\r\n");
                    }
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (!step1.Executed || step1.Response is null)
                    return TestVerdict.Error;

                if (step1.Response.StatusCode is < 200 or >= 300)
                    return TestVerdict.Error;

                var etag = GetHeader(step1, "ETag");
                if (etag is null)
                    return TestVerdict.Warn; // No ETag support

                if (!step2.Executed || step2.Response is null)
                    return TestVerdict.Warn; // Connection closed before step 2

                if (step2.Response.StatusCode == 304)
                    return TestVerdict.Pass;

                if (step2.Response.StatusCode is >= 200 and < 300)
                    return TestVerdict.Warn; // Server ignores If-None-Match

                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                if (!step1.Executed || step1.Response is null) return "Step 1 failed";
                var etag = GetHeader(step1, "ETag");
                if (etag is null) return "No ETag header in response";
                var step2 = steps[1];
                if (!step2.Executed || step2.Response is null) return "Connection closed before conditional request";
                return $"ETag: {etag} → {step2.Response.StatusCode}";
            }
        };

        // ── CAP-LAST-MODIFIED-304 ─────────────────────────────────────
        yield return new SequenceTestCase
        {
            Id = "CAP-LAST-MODIFIED-304",
            Description = "Last-Modified conditional GET returns 304 Not Modified",
            Category = TestCategory.Capabilities,
            Scored = false,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9110 §13.1.3",
            Expected = new ExpectedBehavior { Description = "304" },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Initial GET (capture Last-Modified)",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nConnection: keep-alive\r\n\r\n")
                },
                new SequenceStep
                {
                    Label = "Conditional GET (If-Modified-Since)",
                    DynamicPayloadFactory = (ctx, previousSteps) =>
                    {
                        var lm = GetHeader(previousSteps[0], "Last-Modified") ?? "Thu, 01 Jan 2099 00:00:00 GMT";
                        return MakeRequest(
                            $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nIf-Modified-Since: {lm}\r\n\r\n");
                    }
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (!step1.Executed || step1.Response is null)
                    return TestVerdict.Error;

                if (step1.Response.StatusCode is < 200 or >= 300)
                    return TestVerdict.Error;

                var lm = GetHeader(step1, "Last-Modified");
                if (lm is null)
                    return TestVerdict.Warn; // No Last-Modified support

                if (!step2.Executed || step2.Response is null)
                    return TestVerdict.Warn;

                if (step2.Response.StatusCode == 304)
                    return TestVerdict.Pass;

                if (step2.Response.StatusCode is >= 200 and < 300)
                    return TestVerdict.Warn; // Server ignores If-Modified-Since

                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                if (!step1.Executed || step1.Response is null) return "Step 1 failed";
                var lm = GetHeader(step1, "Last-Modified");
                if (lm is null) return "No Last-Modified header in response";
                var step2 = steps[1];
                if (!step2.Executed || step2.Response is null) return "Connection closed before conditional request";
                return $"Last-Modified: {lm} → {step2.Response.StatusCode}";
            }
        };

        // ── CAP-ETAG-IN-304 ──────────────────────────────────────────
        yield return new SequenceTestCase
        {
            Id = "CAP-ETAG-IN-304",
            Description = "304 response includes ETag header",
            Category = TestCategory.Capabilities,
            Scored = false,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9110 §15.4.5",
            Expected = new ExpectedBehavior { Description = "304 with ETag" },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Initial GET (capture ETag)",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nConnection: keep-alive\r\n\r\n")
                },
                new SequenceStep
                {
                    Label = "Conditional GET (If-None-Match)",
                    DynamicPayloadFactory = (ctx, previousSteps) =>
                    {
                        var etag = GetHeader(previousSteps[0], "ETag") ?? "\"no-etag\"";
                        return MakeRequest(
                            $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nIf-None-Match: {etag}\r\n\r\n");
                    }
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (!step1.Executed || step1.Response is null)
                    return TestVerdict.Error;

                if (step1.Response.StatusCode is < 200 or >= 300)
                    return TestVerdict.Error;

                var etag = GetHeader(step1, "ETag");
                if (etag is null)
                    return TestVerdict.Warn; // No ETag support at all

                if (!step2.Executed || step2.Response is null)
                    return TestVerdict.Warn;

                if (step2.Response.StatusCode != 304)
                    return TestVerdict.Warn; // No conditional support

                var etagIn304 = GetHeader(step2, "ETag");
                return etagIn304 is not null ? TestVerdict.Pass : TestVerdict.Warn;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                if (!step1.Executed || step1.Response is null) return "Step 1 failed";
                var etag = GetHeader(step1, "ETag");
                if (etag is null) return "No ETag support";
                var step2 = steps[1];
                if (!step2.Executed || step2.Response is null) return "Connection closed before conditional request";
                if (step2.Response.StatusCode != 304) return $"Step 2 returned {step2.Response.StatusCode} (no conditional support)";
                var etagIn304 = GetHeader(step2, "ETag");
                return etagIn304 is not null ? $"304 includes ETag: {etagIn304}" : "304 response missing ETag header";
            }
        };

        // ── CAP-INM-PRECEDENCE ────────────────────────────────────────
        yield return new SequenceTestCase
        {
            Id = "CAP-INM-PRECEDENCE",
            Description = "If-None-Match takes precedence over If-Modified-Since",
            Category = TestCategory.Capabilities,
            Scored = false,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9110 §13.1.2",
            Expected = new ExpectedBehavior { Description = "304" },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Initial GET (capture ETag)",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nConnection: keep-alive\r\n\r\n")
                },
                new SequenceStep
                {
                    Label = "Conditional GET (INM + stale IMS)",
                    DynamicPayloadFactory = (ctx, previousSteps) =>
                    {
                        var etag = GetHeader(previousSteps[0], "ETag") ?? "\"no-etag\"";
                        // Use epoch as IMS — far in the past, so IMS alone would NOT produce 304.
                        return MakeRequest(
                            $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nIf-None-Match: {etag}\r\nIf-Modified-Since: Thu, 01 Jan 1970 00:00:00 GMT\r\n\r\n");
                    }
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (!step1.Executed || step1.Response is null)
                    return TestVerdict.Error;

                if (step1.Response.StatusCode is < 200 or >= 300)
                    return TestVerdict.Error;

                var etag = GetHeader(step1, "ETag");
                if (etag is null)
                    return TestVerdict.Warn; // No ETag support

                if (!step2.Executed || step2.Response is null)
                    return TestVerdict.Warn;

                if (step2.Response.StatusCode == 304)
                    return TestVerdict.Pass; // INM matched, IMS ignored

                if (step2.Response.StatusCode is >= 200 and < 300)
                    return TestVerdict.Warn; // Server used IMS (stale) and ignored INM

                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                if (!step1.Executed || step1.Response is null) return "Step 1 failed";
                var etag = GetHeader(step1, "ETag");
                if (etag is null) return "No ETag support";
                var step2 = steps[1];
                if (!step2.Executed || step2.Response is null) return "Connection closed before conditional request";
                if (step2.Response.StatusCode == 304) return "If-None-Match took precedence (correct)";
                if (step2.Response.StatusCode is >= 200 and < 300) return "If-Modified-Since took precedence (INM ignored)";
                return $"Unexpected: {step2.Response.StatusCode}";
            }
        };

        // ── CAP-INM-WILDCARD ──────────────────────────────────────────
        yield return new SequenceTestCase
        {
            Id = "CAP-INM-WILDCARD",
            Description = "If-None-Match: * on existing resource returns 304",
            Category = TestCategory.Capabilities,
            Scored = false,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9110 §13.1.2",
            Expected = new ExpectedBehavior { Description = "304" },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Initial GET (confirm 2xx)",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nConnection: keep-alive\r\n\r\n")
                },
                new SequenceStep
                {
                    Label = "Conditional GET (If-None-Match: *)",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nIf-None-Match: *\r\n\r\n")
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (!step1.Executed || step1.Response is null)
                    return TestVerdict.Error;

                if (step1.Response.StatusCode is < 200 or >= 300)
                    return TestVerdict.Error;

                if (!step2.Executed || step2.Response is null)
                    return TestVerdict.Warn;

                if (step2.Response.StatusCode == 304)
                    return TestVerdict.Pass;

                if (step2.Response.StatusCode is >= 200 and < 300)
                    return TestVerdict.Warn; // Server ignores wildcard

                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                if (!step1.Executed || step1.Response is null) return "Step 1 failed";
                if (step1.Response.StatusCode is < 200 or >= 300) return $"Step 1: {step1.Response.StatusCode}";
                var step2 = steps[1];
                if (!step2.Executed || step2.Response is null) return "Connection closed before conditional request";
                if (step2.Response.StatusCode == 304) return "Wildcard If-None-Match recognized";
                if (step2.Response.StatusCode is >= 200 and < 300) return "Server ignores If-None-Match: *";
                return $"Unexpected: {step2.Response.StatusCode}";
            }
        };

        // ── CAP-IMS-FUTURE ──────────────────────────────────────────────
        yield return new SequenceTestCase
        {
            Id = "CAP-IMS-FUTURE",
            Description = "If-Modified-Since with future date ignored",
            Category = TestCategory.Capabilities,
            Scored = false,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9110 §13.1.3",
            Expected = new ExpectedBehavior { Description = "200" },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Initial GET (confirm 2xx)",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nConnection: keep-alive\r\n\r\n")
                },
                new SequenceStep
                {
                    Label = "Conditional GET (If-Modified-Since: future date)",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nIf-Modified-Since: Thu, 01 Jan 2099 00:00:00 GMT\r\n\r\n")
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (!step1.Executed || step1.Response is null)
                    return TestVerdict.Error;

                if (step1.Response.StatusCode is < 200 or >= 300)
                    return TestVerdict.Error;

                if (!step2.Executed || step2.Response is null)
                    return TestVerdict.Warn;

                if (step2.Response.StatusCode is >= 200 and < 300)
                    return TestVerdict.Pass; // Server correctly ignores future IMS

                if (step2.Response.StatusCode == 304)
                    return TestVerdict.Warn; // Server didn't validate date

                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                if (!step1.Executed || step1.Response is null) return "Step 1 failed";
                if (step1.Response.StatusCode is < 200 or >= 300) return $"Step 1: {step1.Response.StatusCode}";
                var step2 = steps[1];
                if (!step2.Executed || step2.Response is null) return "Connection closed before conditional request";
                if (step2.Response.StatusCode is >= 200 and < 300) return "Correctly ignored future If-Modified-Since";
                if (step2.Response.StatusCode == 304) return "Server returned 304 for future date (didn't validate)";
                return $"Unexpected: {step2.Response.StatusCode}";
            }
        };

        // ── CAP-IMS-INVALID ─────────────────────────────────────────────
        yield return new SequenceTestCase
        {
            Id = "CAP-IMS-INVALID",
            Description = "If-Modified-Since with garbage date ignored",
            Category = TestCategory.Capabilities,
            Scored = false,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9110 §13.1.3",
            Expected = new ExpectedBehavior { Description = "200" },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Initial GET (confirm 2xx)",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nConnection: keep-alive\r\n\r\n")
                },
                new SequenceStep
                {
                    Label = "Conditional GET (If-Modified-Since: garbage)",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nIf-Modified-Since: not-a-date\r\n\r\n")
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (!step1.Executed || step1.Response is null)
                    return TestVerdict.Error;

                if (step1.Response.StatusCode is < 200 or >= 300)
                    return TestVerdict.Error;

                if (!step2.Executed || step2.Response is null)
                    return TestVerdict.Warn;

                if (step2.Response.StatusCode is >= 200 and < 300)
                    return TestVerdict.Pass; // Server correctly ignores invalid date

                if (step2.Response.StatusCode == 304)
                    return TestVerdict.Warn; // Server treated garbage as valid

                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                if (!step1.Executed || step1.Response is null) return "Step 1 failed";
                if (step1.Response.StatusCode is < 200 or >= 300) return $"Step 1: {step1.Response.StatusCode}";
                var step2 = steps[1];
                if (!step2.Executed || step2.Response is null) return "Connection closed before conditional request";
                if (step2.Response.StatusCode is >= 200 and < 300) return "Correctly ignored invalid If-Modified-Since";
                if (step2.Response.StatusCode == 304) return "Server returned 304 for garbage date (treated as valid)";
                return $"Unexpected: {step2.Response.StatusCode}";
            }
        };

        // ── CAP-INM-UNQUOTED ────────────────────────────────────────────
        yield return new SequenceTestCase
        {
            Id = "CAP-INM-UNQUOTED",
            Description = "If-None-Match with unquoted ETag",
            Category = TestCategory.Capabilities,
            Scored = false,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9110 §8.8.3",
            Expected = new ExpectedBehavior { Description = "200" },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Initial GET (capture ETag)",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nConnection: keep-alive\r\n\r\n")
                },
                new SequenceStep
                {
                    Label = "Conditional GET (If-None-Match: unquoted)",
                    DynamicPayloadFactory = (ctx, previousSteps) =>
                    {
                        var etag = GetHeader(previousSteps[0], "ETag");
                        if (etag is null)
                            return MakeRequest(
                                $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nIf-None-Match: no-etag\r\n\r\n");

                        // Strip surrounding quotes (and optional W/ prefix)
                        var stripped = etag;
                        if (stripped.StartsWith("W/"))
                            stripped = stripped[2..];
                        if (stripped.StartsWith('"') && stripped.EndsWith('"'))
                            stripped = stripped[1..^1];

                        return MakeRequest(
                            $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nIf-None-Match: {stripped}\r\n\r\n");
                    }
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (!step1.Executed || step1.Response is null)
                    return TestVerdict.Error;

                if (step1.Response.StatusCode is < 200 or >= 300)
                    return TestVerdict.Error;

                var etag = GetHeader(step1, "ETag");
                if (etag is null)
                    return TestVerdict.Warn; // No ETag support

                if (!step2.Executed || step2.Response is null)
                    return TestVerdict.Warn;

                if (step2.Response.StatusCode is >= 200 and < 300)
                    return TestVerdict.Pass; // Correctly rejects malformed ETag syntax

                if (step2.Response.StatusCode == 304)
                    return TestVerdict.Warn; // Accepted unquoted ETag

                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                if (!step1.Executed || step1.Response is null) return "Step 1 failed";
                var etag = GetHeader(step1, "ETag");
                if (etag is null) return "No ETag support";
                var step2 = steps[1];
                if (!step2.Executed || step2.Response is null) return "Connection closed before conditional request";
                if (step2.Response.StatusCode is >= 200 and < 300) return "Correctly rejected unquoted ETag syntax";
                if (step2.Response.StatusCode == 304) return "Accepted unquoted ETag (lenient parsing)";
                return $"Unexpected: {step2.Response.StatusCode}";
            }
        };

        // ── CAP-ETAG-WEAK ───────────────────────────────────────────────
        yield return new SequenceTestCase
        {
            Id = "CAP-ETAG-WEAK",
            Description = "Weak ETag comparison for GET",
            Category = TestCategory.Capabilities,
            Scored = false,
            RfcLevel = RfcLevel.Should,
            RfcReference = "RFC 9110 §13.1.2",
            Expected = new ExpectedBehavior { Description = "304" },
            Steps =
            [
                new SequenceStep
                {
                    Label = "Initial GET (capture ETag)",
                    PayloadFactory = ctx => MakeRequest(
                        $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nConnection: keep-alive\r\n\r\n")
                },
                new SequenceStep
                {
                    Label = "Conditional GET (If-None-Match: W/etag)",
                    DynamicPayloadFactory = (ctx, previousSteps) =>
                    {
                        var etag = GetHeader(previousSteps[0], "ETag");
                        if (etag is null)
                            return MakeRequest(
                                $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nIf-None-Match: W/\"no-etag\"\r\n\r\n");

                        // If already weak, send as-is; if strong, prepend W/
                        var weakEtag = etag.StartsWith("W/") ? etag : $"W/{etag}";

                        return MakeRequest(
                            $"GET / HTTP/1.1\r\nHost: {ctx.HostHeader}\r\nIf-None-Match: {weakEtag}\r\n\r\n");
                    }
                }
            ],
            Validator = steps =>
            {
                var step1 = steps[0];
                var step2 = steps[1];

                if (!step1.Executed || step1.Response is null)
                    return TestVerdict.Error;

                if (step1.Response.StatusCode is < 200 or >= 300)
                    return TestVerdict.Error;

                var etag = GetHeader(step1, "ETag");
                if (etag is null)
                    return TestVerdict.Warn; // No ETag support

                if (!step2.Executed || step2.Response is null)
                    return TestVerdict.Warn;

                if (step2.Response.StatusCode == 304)
                    return TestVerdict.Pass; // Weak comparison matched

                if (step2.Response.StatusCode is >= 200 and < 300)
                    return TestVerdict.Warn; // Server didn't match weak ETag

                return TestVerdict.Fail;
            },
            BehavioralAnalyzer = steps =>
            {
                var step1 = steps[0];
                if (!step1.Executed || step1.Response is null) return "Step 1 failed";
                var etag = GetHeader(step1, "ETag");
                if (etag is null) return "No ETag support";
                var step2 = steps[1];
                if (!step2.Executed || step2.Response is null) return "Connection closed before conditional request";
                var weakEtag = etag.StartsWith("W/") ? etag : $"W/{etag}";
                if (step2.Response.StatusCode == 304) return $"Weak comparison matched: {weakEtag} → 304";
                if (step2.Response.StatusCode is >= 200 and < 300) return $"Weak comparison not matched: {weakEtag} → {step2.Response.StatusCode}";
                return $"Unexpected: {step2.Response.StatusCode}";
            }
        };
    }
}
