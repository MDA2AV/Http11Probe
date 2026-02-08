---
title: Request Line
description: "Request Line — Http11Probe documentation"
weight: 3
sidebar:
  open: false
---

The request-line is the first line of an HTTP request: `method SP request-target SP HTTP-version CRLF`. RFC 9112 Section 3 defines its grammar strictly. Malformed request-lines are a common vector for parser confusion.

## The Rule

> "Recipients of an invalid request-line **SHOULD** respond with either a 400 (Bad Request) error or a 301 (Moved Permanently) redirect with the request-target properly encoded." — RFC 9112 Section 3

Note this is a SHOULD, not a MUST. The RFC recommends 400 but does not mandate it — closing the connection is also acceptable.

## Tests

{{< cards >}}
  {{< card link="multi-sp-request-line" title="MULTI-SP-REQUEST-LINE" subtitle="Multiple spaces between method, target, and version." >}}
  {{< card link="missing-target" title="MISSING-TARGET" subtitle="Request-line with no request-target." >}}
  {{< card link="fragment-in-target" title="FRAGMENT-IN-TARGET" subtitle="Fragment identifier (#) in request-target." >}}
  {{< card link="invalid-version" title="INVALID-VERSION" subtitle="Unrecognized HTTP version string." >}}
  {{< card link="http09-request" title="HTTP09-REQUEST" subtitle="HTTP/0.9 style request with no version." >}}
  {{< card link="asterisk-with-get" title="ASTERISK-WITH-GET" subtitle="Asterisk-form (*) with non-OPTIONS method." >}}
  {{< card link="options-star" title="OPTIONS-STAR" subtitle="OPTIONS * — valid asterisk-form request." >}}
  {{< card link="unknown-te-501" title="UNKNOWN-TE-501" subtitle="Unknown Transfer-Encoding without CL." >}}
  {{< card link="connect-empty-port" title="CONNECT-EMPTY-PORT" subtitle="CONNECT with empty port in authority-form." >}}
{{< /cards >}}

### Unscored

{{< cards >}}
  {{< card link="absolute-form" title="ABSOLUTE-FORM" subtitle="Absolute-form request-target (http://host/)." >}}
  {{< card link="method-case" title="METHOD-CASE" subtitle="Lowercase method 'get'. Methods are case-sensitive." >}}
{{< /cards >}}
