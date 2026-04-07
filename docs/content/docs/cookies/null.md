---
title: "NUL"
description: "COOK-NUL cookie test documentation"
weight: 4
---

| | |
|---|---|
| **Test ID** | `COOK-NUL` |
| **Category** | Cookies |
| **Scored** | No |
| **RFC Level** | N/A |
| **Expected** | `400 (rejected) or 2xx without NUL` |

## What it sends

NUL byte in cookie value — dangerous if preserved by parser.

```http
GET /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: foo=\0bar\r\n
\r\n
```

The cookie value contains a NUL byte (`0x00`).

## Why it matters

NUL bytes in cookie values can truncate strings in C-based parsers, cause log injection, or enable header injection if the NUL terminates a string boundary check.

## Verdicts

- **Pass** — 400 rejected, or 2xx with NUL stripped
- **Fail** — 2xx with NUL byte preserved in output (dangerous), or 500

## Sources

- [RFC 6265 §5.4](https://www.rfc-editor.org/rfc/rfc6265#section-5.4) — Cookie header
