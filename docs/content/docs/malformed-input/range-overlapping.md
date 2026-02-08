---
title: "RANGE-OVERLAPPING"
description: "RANGE-OVERLAPPING test documentation"
weight: 25
---

| | |
|---|---|
| **Test ID** | `MAL-RANGE-OVERLAPPING` (unscored) |
| **Category** | Malformed Input |
| **RFC** | [RFC 9110 Section 14.2](https://www.rfc-editor.org/rfc/rfc9110#section-14.2) |
| **Expected** | Any response = Warn |

## What it sends

A GET request with a Range header containing 1,000 overlapping range values.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Range: bytes=0-,0-,0-,...{1000 total}...\r\n
\r\n
```

The `Range` header contains 1,000 repetitions of `0-`, each requesting the entire resource.

## What the RFC says

RFC 9110 Section 14.2 defines the Range header. The RFC does not prohibit overlapping or duplicated range values. Servers have broad discretion in how they respond:

> "The 400 (Bad Request) status code indicates that the server cannot or will not process the request due to something that is perceived to be a client error." — RFC 9110 Section 15.5.1

A server may respond with 200 (ignoring the Range header), 206 (Partial Content), 416 (Range Not Satisfiable), or 400 (rejecting the request). All are valid approaches.

## Why this test is unscored

The RFC does not forbid overlapping ranges. A server may legitimately respond with `200` (ignoring the Range header), `206` (honoring the ranges), `400` (rejecting as abusive), or `416` (Range Not Satisfiable). Because all of these responses are defensible, no single outcome can be graded as correct, so this test is reported as Warn for any response.

## Why it matters

CVE-2011-3192 (Apache Range header DoS) showed that servers that expand each range independently can consume massive memory. A single request with 1,000 overlapping ranges could cause the server to generate a multipart response containing thousands of copies of the same content, exhausting memory and CPU. A robust server should either ignore the Range header, merge overlapping ranges, or reject the request.

## Deep Analysis

### Relevant ABNF

```
Range                  = ranges-specifier
ranges-specifier       = range-unit "=" range-set
range-set              = 1#range-spec
range-spec             = int-range / suffix-range / other-range
int-range              = first-pos "-" [ last-pos ]
byte-ranges-specifier  = bytes-unit "=" byte-range-set
byte-range-set         = 1#( byte-range-spec / suffix-byte-range-spec )
byte-range-spec        = first-byte-pos "-" [ last-byte-pos ]
```

### RFC Evidence

> "A server that supports range requests MAY ignore or reject a Range header field that contains an invalid ranges-specifier, a ranges-specifier with more than two overlapping ranges, or a set of many small ranges that are not listed in ascending order, since these are indications of either a broken client or a deliberate denial-of-service attack."
> -- RFC 9110 Section 14.2

> "The 'Range' header field on a GET request modifies the method semantics to request transfer of only one or more subranges of the selected representation data, rather than the entire selected representation."
> -- RFC 9110 Section 14.2

> "A recipient SHOULD parse a received protocol element defensively, with only marginal expectations that the element will conform to its ABNF grammar and fit within a reasonable buffer size."
> -- RFC 9110 Section 2.3

### Chain of Reasoning

1. **The request is syntactically valid.** Each `0-` range spec conforms to `byte-range-spec = first-byte-pos "-" [ last-byte-pos ]` where `first-byte-pos` is `0` and `last-byte-pos` is omitted (meaning "to the end"). The comma-separated list satisfies the `1#` (one or more) list rule.

2. **This test is unscored because the RFC explicitly permits ignoring Range.** RFC 9110 Section 14.2 states servers "MAY ignore or reject" a Range header that indicates "a deliberate denial-of-service attack." Since the RFC uses MAY-level language, there is no single correct behavior: a server may ignore the Range header and serve a normal 200 response, merge the overlapping ranges into a single range, reject the request with 400 or 416, or close the connection. All behaviors are RFC-compliant, which is why this test cannot be objectively scored as Pass or Fail.

3. **1,000 identical `0-` ranges is a clear DoS indicator.** The RFC specifically calls out "a set of many small ranges" as an indication of "a deliberate denial-of-service attack." While the test uses overlapping full-resource ranges rather than small ranges, the principle is the same: the number of ranges is unreasonable.

4. **CVE-2011-3192 demonstrated the real-world impact.** The Apache HTTPD "Range header DoS" vulnerability (also known as the "Apache Killer") allowed a single request with many overlapping byte ranges to cause Apache to generate a multipart response containing thousands of copies of the resource content, exhausting memory and CPU. This CVE directly motivates this test case.

5. **Any response earns Warn.** Because the test is unscored, the probe records a Warn regardless of the server's response. The purpose is informational: to observe how the server handles this known attack pattern, not to enforce a specific behavior.

## Sources

- [RFC 9110 Section 14.2](https://www.rfc-editor.org/rfc/rfc9110#section-14.2) — Range header field
- [RFC 9110 Section 15.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-15.5.1) — 400 Bad Request
- [CVE-2011-3192](https://nvd.nist.gov/vuln/detail/CVE-2011-3192) — Apache Range header DoS
