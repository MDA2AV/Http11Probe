---
title: "MULTI-HEADER"
description: "COOK-MULTI-HEADER test documentation"
weight: 8
---

| | |
|---|---|
| **Test ID** | `COOK-MULTI-HEADER` |
| **Category** | Cookies |
| **Scored** | No |
| **Expected** | `2xx` with both cookies |

## What it sends

```http
GET /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: a=1\r\n
Cookie: b=2\r\n
\r\n
```

Two separate `Cookie` header lines in the same request.

## What the RFC says

> "If the user agent does attach a Cookie header field to an HTTP request, the user agent MUST NOT attach more than one header field named Cookie." — RFC 6265 §5.4

> "If a server receives multiple Cookie header field lines in a single request... the server SHOULD treat them as if they had been sent as a single cookie-string separated by semicolons." — RFC 6265 §5.3 (revised in RFC 6265bis)

While clients MUST NOT send multiple Cookie headers, servers should handle them gracefully by folding them together.

## Why it matters

Multiple `Cookie` headers can occur in practice through proxy manipulation or misconfigured middleware. A server that crashes or drops cookies when it sees duplicates is fragile. The ideal behavior is to fold them into a single cookie-string as RFC 6265bis recommends.

## Verdicts

- **Pass** — `2xx` with both `a=1` and `b=2` in the echo body
- **Warn** — Only one cookie echoed, or `400` (rejected but didn't crash)
- **Fail** — `500` or connection crash

## Sources

- [RFC 6265 §5.4](https://www.rfc-editor.org/rfc/rfc6265#section-5.4) — sending cookies
- [RFC 6265 §5.3](https://www.rfc-editor.org/rfc/rfc6265#section-5.3) — cookie processing
