---
title: "NUL"
description: "COOK-NUL test documentation"
weight: 4
---

| | |
|---|---|
| **Test ID** | `COOK-NUL` |
| **Category** | Cookies |
| **Scored** | No |
| **Expected** | `400` (rejected) or `2xx` without NUL |

## What it sends

```http
GET /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: foo=\x00bar\r\n
\r\n
```

A `Cookie` header containing a NUL byte (`0x00`) embedded in the cookie value.

## What the RFC says

> "Field values containing CR, LF, or NUL characters are invalid and dangerous... a recipient of CR, LF, or NUL within a field value MUST either reject the message or replace each of those characters with SP before further processing." — RFC 9110 §5.5

NUL bytes in cookie values are not valid in any HTTP header field.

## Why it matters

NUL bytes in cookie values are a serious security concern. If a cookie parser preserves the NUL byte, it can:
- **Truncate strings** in C-based parsers, causing the cookie value to appear shorter than it is
- **Bypass security filters** that stop reading at NUL
- **Corrupt downstream processing** in systems that interpret NUL as a string terminator

## Verdicts

- **Pass** — `400` (rejected) or `2xx` with NUL stripped/cookie dropped
- **Fail** — `2xx` with NUL byte preserved in the response body (dangerous)

## Sources

- [RFC 9110 §5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5) — field values with NUL
- [RFC 6265 §4.1.1](https://www.rfc-editor.org/rfc/rfc6265#section-4.1.1) — cookie-value syntax
