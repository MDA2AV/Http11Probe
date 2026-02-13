---
title: Request Smuggling
description: "Request Smuggling — Http11Probe documentation"
weight: 10
sidebar:
  open: false
---

HTTP request smuggling exploits disagreements between HTTP processors about where one request ends and the next begins. When two servers parse the same byte stream differently, an attacker can hide a request inside another.

## How Smuggling Works

In a typical architecture, requests flow through layers:

```
Client  →  CDN / Load Balancer  →  Application Server
           (front-end)              (back-end)
```

Both must parse the HTTP stream to determine request boundaries. HTTP/1.1 provides two mechanisms:

1. **Content-Length** — an explicit byte count of the body
2. **Transfer-Encoding: chunked** — body sent in length-prefixed chunks

When a request contains both headers, or contains them in ambiguous forms, different servers may disagree on the body length. This lets an attacker smuggle a second request inside the first.

### CL.TE Example

```http
POST / HTTP/1.1
Host: example.com
Content-Length: 13
Transfer-Encoding: chunked

0\r\n\r\nSMUGGLED
```

**Front-end** (uses CL: 13): forwards all 13 bytes as one request.
**Back-end** (uses TE: chunked): reads chunk `0` (end), leaves `SMUGGLED` as the next request.

### Real-World Impact

- **Bypass authentication** — smuggle requests past WAF/auth layers
- **Poison web caches** — inject responses cached for other users
- **Hijack sessions** — prepend attacker headers to other users' requests
- **Exfiltrate data** — redirect internal responses to attacker endpoints

## Scored vs Unscored

Some smuggling tests are **unscored** because the RFC permits multiple interpretations:
- OWS trimming (`Content-Length:  42` with extra space)
- Case-insensitive TE matching (`Chunked` vs `chunked`)
- Trailing whitespace in values

For these, `400` is the strict/safe response and `2xx` is RFC-compliant. Http11Probe shows `2xx` as a warning but does not count it against the score.

## Tests

### Scored

{{< cards >}}
  {{< card link="cl-te-both" title="CL-TE-BOTH" subtitle="Both Content-Length and Transfer-Encoding present." >}}
  {{< card link="duplicate-cl" title="DUPLICATE-CL" subtitle="Two Content-Length headers with different values." >}}
  {{< card link="cl-leading-zeros" title="CL-LEADING-ZEROS" subtitle="Content-Length with leading zeros (007)." >}}
  {{< card link="cl-negative" title="CL-NEGATIVE" subtitle="Negative Content-Length value." >}}
  {{< card link="te-xchunked" title="TE-XCHUNKED" subtitle="Unknown TE 'xchunked' with CL present." >}}
  {{< card link="te-trailing-space" title="TE-TRAILING-SPACE" subtitle="TE 'chunked ' with trailing space." >}}
  {{< card link="te-sp-before-colon" title="TE-SP-BEFORE-COLON" subtitle="Space before colon in Transfer-Encoding." >}}
  {{< card link="clte-pipeline" title="CLTE-PIPELINE" subtitle="Full CL.TE smuggling payload." >}}
  {{< card link="tecl-pipeline" title="TECL-PIPELINE" subtitle="Full TE.CL smuggling payload." >}}
  {{< card link="cl-comma-different" title="CL-COMMA-DIFFERENT" subtitle="Comma-separated CL with different values." >}}
  {{< card link="te-not-final-chunked" title="TE-NOT-FINAL-CHUNKED" subtitle="Chunked is not the final transfer encoding." >}}
  {{< card link="te-http10" title="TE-HTTP10" subtitle="Transfer-Encoding in HTTP/1.0 request." >}}
  {{< card link="chunk-bare-semicolon" title="CHUNK-BARE-SEMICOLON" subtitle="Bare semicolon in chunk size." >}}
  {{< card link="bare-cr-header-value" title="BARE-CR-HEADER-VALUE" subtitle="Bare CR in header value." >}}
  {{< card link="cl-octal" title="CL-OCTAL" subtitle="Content-Length with octal prefix." >}}
  {{< card link="chunk-underscore" title="CHUNK-UNDERSCORE" subtitle="Underscore in chunk size." >}}
  {{< card link="te-empty-value" title="TE-EMPTY-VALUE" subtitle="Empty Transfer-Encoding value with CL." >}}
  {{< card link="te-leading-comma" title="TE-LEADING-COMMA" subtitle="Leading comma in Transfer-Encoding." >}}
  {{< card link="te-duplicate-headers" title="TE-DUPLICATE-HEADERS" subtitle="Two TE headers with conflicting values." >}}
  {{< card link="chunk-hex-prefix" title="CHUNK-HEX-PREFIX" subtitle="Chunk size with 0x prefix." >}}
  {{< card link="cl-hex-prefix" title="CL-HEX-PREFIX" subtitle="Content-Length with 0x prefix." >}}
  {{< card link="cl-internal-space" title="CL-INTERNAL-SPACE" subtitle="Space inside Content-Length value." >}}
  {{< card link="chunk-leading-sp" title="CHUNK-LEADING-SP" subtitle="Leading space in chunk size." >}}
  {{< card link="chunk-missing-trailing-crlf" title="CHUNK-MISSING-TRAILING-CRLF" subtitle="Chunk data without trailing CRLF." >}}
  {{< card link="chunk-ext-lf" title="CHUNK-EXT-LF" subtitle="Bare LF in chunk extension (TERM.EXT vector)." >}}
  {{< card link="chunk-spill" title="CHUNK-SPILL" subtitle="Chunk declares size 5 but sends 7 bytes." >}}
  {{< card link="chunk-lf-term" title="CHUNK-LF-TERM" subtitle="Bare LF as chunk data terminator." >}}
  {{< card link="chunk-ext-ctrl" title="CHUNK-EXT-CTRL" subtitle="NUL byte in chunk extension." >}}
  {{< card link="chunk-ext-cr" title="CHUNK-EXT-CR" subtitle="Bare CR inside chunk extension metadata." >}}
  {{< card link="chunk-lf-trailer" title="CHUNK-LF-TRAILER" subtitle="Bare LF in trailer section termination." >}}
  {{< card link="te-identity" title="TE-IDENTITY" subtitle="Transfer-Encoding: identity (deprecated) with CL." >}}
  {{< card link="te-vtab" title="TE-VTAB" subtitle="Vertical tab before chunked token." >}}
  {{< card link="te-formfeed" title="TE-FORMFEED" subtitle="Form-feed before chunked token." >}}
  {{< card link="te-null" title="TE-NULL" subtitle="NUL byte appended to chunked token." >}}
  {{< card link="chunk-negative" title="CHUNK-NEGATIVE" subtitle="Negative chunk size (-1)." >}}
  {{< card link="chunk-bare-cr-term" title="CHUNK-BARE-CR-TERM" subtitle="Bare CR as chunk size line terminator." >}}
  {{< card link="cl-underscore" title="CL-UNDERSCORE" subtitle="Content-Length with underscore digit separator (1_0)." >}}
  {{< card link="cl-negative-zero" title="CL-NEGATIVE-ZERO" subtitle="Content-Length: -0 — not valid 1*DIGIT." >}}
  {{< card link="cl-double-zero" title="CL-DOUBLE-ZERO" subtitle="Content-Length: 00 — leading zero ambiguity." >}}
  {{< card link="cl-leading-zeros-octal" title="CL-LEADING-ZEROS-OCTAL" subtitle="Content-Length: 0200 — octal vs decimal disagreement." >}}
  {{< card link="te-obs-fold" title="TE-OBS-FOLD" subtitle="Transfer-Encoding with obs-fold line wrapping." >}}
  {{< card link="te-trailing-comma" title="TE-TRAILING-COMMA" subtitle="Transfer-Encoding: chunked, — trailing comma." >}}
  {{< card link="multiple-host-comma" title="MULTIPLE-HOST-COMMA" subtitle="Host with comma-separated values." >}}
{{< /cards >}}

### Unscored

{{< cards >}}
  {{< card link="cl-trailing-space" title="CL-TRAILING-SPACE" subtitle="Trailing space in CL value. OWS trimming is valid." >}}
  {{< card link="cl-extra-leading-sp" title="CL-EXTRA-LEADING-SP" subtitle="Extra space after CL colon. OWS is valid." >}}
  {{< card link="te-double-chunked" title="TE-DOUBLE-CHUNKED" subtitle="Duplicate 'chunked' TE with CL." >}}
  {{< card link="te-case-mismatch" title="TE-CASE-MISMATCH" subtitle="'Chunked' vs 'chunked'. Case is valid per RFC." >}}
  {{< card link="transfer-encoding-underscore" title="TRANSFER_ENCODING" subtitle="Underscore instead of hyphen in header name." >}}
  {{< card link="cl-comma-same" title="CL-COMMA-SAME" subtitle="Comma-separated identical CL values." >}}
  {{< card link="chunked-with-params" title="CHUNKED-WITH-PARAMS" subtitle="Parameters on chunked encoding." >}}
  {{< card link="expect-100-cl" title="EXPECT-100-CL" subtitle="Expect: 100-continue with Content-Length." >}}
  {{< card link="trailer-cl" title="TRAILER-CL" subtitle="Content-Length in chunked trailers (prohibited)." >}}
  {{< card link="trailer-te" title="TRAILER-TE" subtitle="Transfer-Encoding in chunked trailers (prohibited)." >}}
  {{< card link="trailer-host" title="TRAILER-HOST" subtitle="Host header in chunked trailers (must not route)." >}}
  {{< card link="trailer-auth" title="TRAILER-AUTH" subtitle="Authorization in chunked trailers (prohibited)." >}}
  {{< card link="trailer-content-type" title="TRAILER-CONTENT-TYPE" subtitle="Content-Type in chunked trailers (prohibited)." >}}
  {{< card link="head-cl-body" title="HEAD-CL-BODY" subtitle="HEAD with Content-Length and body." >}}
  {{< card link="options-cl-body" title="OPTIONS-CL-BODY" subtitle="OPTIONS with Content-Length and body." >}}
  {{< card link="te-tab-before-value" title="TE-TAB-BEFORE-VALUE" subtitle="Tab as OWS before Transfer-Encoding value." >}}
  {{< card link="absolute-uri-host-mismatch" title="ABSOLUTE-URI-HOST-MISMATCH" subtitle="Absolute-form URI with different Host header." >}}
{{< /cards >}}
