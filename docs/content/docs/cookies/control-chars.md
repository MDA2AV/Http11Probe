---
title: "CONTROL-CHARS"
description: "COOK-CONTROL-CHARS cookie test documentation"
weight: 5
---

| | |
|---|---|
| **Test ID** | `COOK-CONTROL-CHARS` |
| **Category** | Cookies |
| **Scored** | No |
| **RFC Level** | N/A |
| **Expected** | `400 (rejected) or 2xx without control chars` |

## What it sends

Control characters (0x01-0x03) in cookie value — dangerous if preserved.

```http
GET /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: foo=\x01\x02\x03\r\n
\r\n
```

## Why it matters

Control characters in cookie values violate RFC 6265's cookie-octet grammar and can enable response splitting or log injection if passed through to output.

## Verdicts

- **Pass** — 400 rejected, or 2xx with control chars stripped
- **Fail** — 2xx with control chars preserved (dangerous), or 500

## Sources

- [RFC 6265 §5.4](https://www.rfc-editor.org/rfc/rfc6265#section-5.4) — Cookie header
