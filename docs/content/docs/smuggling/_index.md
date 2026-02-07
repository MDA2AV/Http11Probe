---
title: Request Smuggling
weight: 7
sidebar:
  open: true
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
{{< /cards >}}

### Unscored

{{< cards >}}
  {{< card link="cl-trailing-space" title="CL-TRAILING-SPACE" subtitle="Trailing space in CL value. OWS trimming is valid." >}}
  {{< card link="cl-extra-leading-sp" title="CL-EXTRA-LEADING-SP" subtitle="Extra space after CL colon. OWS is valid." >}}
  {{< card link="header-injection" title="HEADER-INJECTION" subtitle="CRLF injection in header value." >}}
  {{< card link="te-double-chunked" title="TE-DOUBLE-CHUNKED" subtitle="Duplicate 'chunked' TE with CL." >}}
  {{< card link="te-case-mismatch" title="TE-CASE-MISMATCH" subtitle="'Chunked' vs 'chunked'. Case is valid per RFC." >}}
{{< /cards >}}
