namespace Http11Probe.Cli.Reporting;

internal static class DocsUrlMap
{
    private const string BaseUrl = "https://mda2av.github.io/Http11Probe/docs/";

    private static readonly Dictionary<string, string> ComplianceSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        // body
        ["COMP-CHUNKED-BODY"] = "body/chunked-body",
        ["COMP-CHUNKED-EMPTY"] = "body/chunked-empty",
        ["COMP-CHUNKED-EXTENSION"] = "body/chunked-extension",
        ["COMP-CHUNKED-HEX-UPPERCASE"] = "body/chunked-hex-uppercase",
        ["COMP-CHUNKED-MULTI"] = "body/chunked-multi",
        ["COMP-CHUNKED-NO-FINAL"] = "body/chunked-no-final",
        ["COMP-CHUNKED-TRAILER-VALID"] = "body/chunked-trailer-valid",
        ["COMP-GET-WITH-CL-BODY"] = "body/get-with-cl-body",
        ["COMP-POST-CL-BODY"] = "body/post-cl-body",
        ["COMP-POST-CL-UNDERSEND"] = "body/post-cl-undersend",
        ["COMP-POST-CL-ZERO"] = "body/post-cl-zero",
        ["COMP-POST-NO-CL-NO-TE"] = "body/post-no-cl-no-te",
        ["COMP-POST-UNSUPPORTED-CT"] = "body/post-unsupported-ct",

        // content-length
        ["RFC9112-6.1-CL-NON-NUMERIC"] = "content-length/cl-non-numeric",
        ["RFC9112-6.1-CL-PLUS-SIGN"] = "content-length/cl-plus-sign",
        ["COMP-NO-CL-IN-204"] = "content-length/no-cl-in-204",

        // headers
        ["COMP-ACCEPT-NONSENSE"] = "headers/accept-nonsense",
        ["COMP-CONNECTION-CLOSE"] = "headers/connection-close",
        ["COMP-CONTENT-TYPE"] = "headers/content-type-presence",
        ["COMP-DATE-FORMAT"] = "headers/date-format",
        ["COMP-DATE-HEADER"] = "headers/date-header",
        ["COMP-DUPLICATE-CT"] = "headers/duplicate-ct",
        ["RFC9112-5-EMPTY-HEADER-NAME"] = "headers/empty-header-name",
        ["COMP-EXPECT-UNKNOWN"] = "headers/expect-unknown",
        ["RFC9112-5-HEADER-NO-COLON"] = "headers/header-no-colon",
        ["COMP-HTTP10-DEFAULT-CLOSE"] = "headers/http10-default-close",
        ["COMP-NO-1XX-HTTP10"] = "headers/no-1xx-http10",
        ["RFC9112-5-INVALID-HEADER-NAME"] = "headers/invalid-header-name",
        ["RFC9112-5.1-OBS-FOLD"] = "headers/obs-fold",
        ["RFC9110-5.6.2-SP-BEFORE-COLON"] = "headers/sp-before-colon",
        ["COMP-WHITESPACE-BEFORE-HEADERS"] = "headers/whitespace-before-headers",

        // host-header
        ["RFC9110-5.4-DUPLICATE-HOST"] = "host-header/duplicate-host",
        ["COMP-DUPLICATE-HOST-SAME"] = "host-header/duplicate-host-same",
        ["COMP-HOST-EMPTY-VALUE"] = "host-header/host-empty-value",
        ["COMP-HOST-WITH-PATH"] = "host-header/host-with-path",
        ["COMP-HOST-WITH-USERINFO"] = "host-header/host-with-userinfo",
        ["COMP-HTTP10-NO-HOST"] = "host-header/http10-no-host",
        ["RFC9112-7.1-MISSING-HOST"] = "host-header/missing-host",

        // line-endings
        ["RFC9112-2.2-BARE-LF-HEADER"] = "line-endings/bare-lf-header",
        ["RFC9112-2.2-BARE-LF-REQUEST-LINE"] = "line-endings/bare-lf-request-line",
        ["RFC9112-3-CR-ONLY-LINE-ENDING"] = "line-endings/cr-only-line-ending",
        ["COMP-LEADING-CRLF"] = "line-endings/leading-crlf",

        // request-line
        ["COMP-405-ALLOW"] = "request-line/405-allow",
        ["COMP-ABSOLUTE-FORM"] = "request-line/absolute-form",
        ["COMP-ASTERISK-WITH-GET"] = "request-line/asterisk-with-get",
        ["RFC9112-3.2-FRAGMENT-IN-TARGET"] = "request-line/fragment-in-target",
        ["RFC9112-2.3-HTTP09-REQUEST"] = "request-line/http09-request",
        ["COMP-HTTP12-VERSION"] = "request-line/http12-version",
        ["RFC9112-2.3-INVALID-VERSION"] = "request-line/invalid-version",
        ["COMP-METHOD-CASE"] = "request-line/method-case",
        ["COMP-HEAD-NO-BODY"] = "request-line/head-no-body",
        ["COMP-METHOD-CONNECT"] = "request-line/method-connect",
        ["COMP-METHOD-TRACE"] = "request-line/method-trace",
        ["RFC9112-3-MISSING-TARGET"] = "request-line/missing-target",
        ["RFC9112-3-MULTI-SP-REQUEST-LINE"] = "request-line/multi-sp-request-line",
        ["COMP-OPTIONS-ALLOW"] = "request-line/options-allow",
        ["COMP-OPTIONS-STAR"] = "request-line/options-star",
        ["COMP-REQUEST-LINE-TAB"] = "request-line/request-line-tab",
        ["COMP-SPACE-IN-TARGET"] = "request-line/space-in-target",
        ["COMP-TRACE-SENSITIVE"] = "request-line/trace-sensitive",
        ["COMP-TRACE-WITH-BODY"] = "request-line/trace-with-body",
        ["COMP-UNKNOWN-METHOD"] = "request-line/unknown-method",
        ["COMP-UNKNOWN-TE-501"] = "request-line/unknown-te-501",
        ["COMP-VERSION-CASE"] = "request-line/version-case",
        ["COMP-VERSION-LEADING-ZEROS"] = "request-line/version-leading-zeros",
        ["COMP-VERSION-MISSING-MINOR"] = "request-line/version-missing-minor",
        ["COMP-VERSION-WHITESPACE"] = "request-line/version-whitespace",
        ["COMP-LONG-URL-OK"] = "request-line/long-url-ok",

        // range
        ["COMP-RANGE-INVALID"] = "body/range-invalid",
        ["COMP-RANGE-POST"] = "body/range-post",

        // upgrade
        ["COMP-UPGRADE-HTTP10"] = "upgrade/upgrade-http10",
        ["COMP-UPGRADE-INVALID-VER"] = "upgrade/upgrade-invalid-ver",
        ["COMP-UPGRADE-MISSING-CONN"] = "upgrade/upgrade-missing-conn",
        ["COMP-UPGRADE-POST"] = "upgrade/upgrade-post",
        ["COMP-UPGRADE-UNKNOWN"] = "upgrade/upgrade-unknown",
    };

    // Special cases where the doc filename doesn't match the ID suffix
    private static readonly Dictionary<string, string> SpecialSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["COMP-BASELINE"] = "baseline",
        ["MAL-CHUNK-EXT-64K"] = "malformed-input/chunk-extension-long",
        ["SMUG-TRANSFER_ENCODING"] = "smuggling/transfer-encoding-underscore",
    };

    public static string? GetUrl(string testId)
    {
        if (SpecialSlugs.TryGetValue(testId, out var special))
            return BaseUrl + special;

        if (ComplianceSlugs.TryGetValue(testId, out var slug))
            return BaseUrl + slug;

        // SMUG-* → smuggling/{suffix}
        if (testId.StartsWith("SMUG-", StringComparison.OrdinalIgnoreCase))
        {
            var suffix = testId[5..].ToLowerInvariant();
            return BaseUrl + "smuggling/" + suffix;
        }

        // MAL-* → malformed-input/{suffix}
        if (testId.StartsWith("MAL-", StringComparison.OrdinalIgnoreCase))
        {
            var suffix = testId[4..].ToLowerInvariant();
            return BaseUrl + "malformed-input/" + suffix;
        }

        // NORM-* → normalization/{suffix}
        if (testId.StartsWith("NORM-", StringComparison.OrdinalIgnoreCase))
        {
            var suffix = testId[5..].ToLowerInvariant();
            return BaseUrl + "normalization/" + suffix;
        }

        // COOK-* → cookies/{suffix}
        if (testId.StartsWith("COOK-", StringComparison.OrdinalIgnoreCase))
        {
            var suffix = testId[5..].ToLowerInvariant();
            return BaseUrl + "cookies/" + suffix;
        }

        return null;
    }
}
