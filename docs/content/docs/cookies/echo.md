---
title: "ECHO"
description: "COOK-ECHO test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `COOK-ECHO` |
| **Category** | Cookies |
| **Scored** | No |
| **Expected** | `2xx` with `Cookie:` in echo body |

## What it sends

```http
GET /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: foo=bar\r\n
\r\n
```

A standard request with a simple, valid `Cookie` header targeting the `/echo` endpoint.

## What the RFC says

> "When the user agent generates an HTTP request, the user agent MUST NOT attach more than one header field named Cookie." — RFC 6265 §5.4

This test sends a single, well-formed `Cookie` header. It serves as a baseline to confirm the echo endpoint reflects cookie headers.

## Why it matters

This is the baseline cookie test. If the server cannot echo back a simple `Cookie: foo=bar` header, all other cookie tests are meaningless.

## Verdicts

- **Pass** — 2xx response with `Cookie:` visible in the echo body
- **Fail** — No response, or 2xx without the cookie header in the body

## Sources

- [RFC 6265 §5.4](https://www.rfc-editor.org/rfc/rfc6265#section-5.4) — sending cookies
