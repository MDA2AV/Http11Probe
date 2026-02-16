---
title: "CONTROL-CHARS"
description: "COOK-CONTROL-CHARS test documentation"
weight: 5
---

| | |
|---|---|
| **Test ID** | `COOK-CONTROL-CHARS` |
| **Category** | Cookies |
| **Scored** | No |
| **Expected** | `400` (rejected) or `2xx` without control chars |

## What it sends

```http
GET /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: foo=\x01\x02\x03\r\n
\r\n
```

A `Cookie` header containing control characters SOH (`0x01`), STX (`0x02`), and ETX (`0x03`) as the cookie value.

## What the RFC says

> "cookie-octet = %x21 / %x23-2B / %x2D-3A / %x3C-5B / %x5D-7E" — RFC 6265 §4.1.1

Control characters (`0x00-0x1F`) are explicitly excluded from the `cookie-octet` production. They are not valid in cookie values.

## Why it matters

Control characters in cookie values can cause:
- **Log injection** — if the bytes reach log files, they may corrupt formatting or inject terminal escape sequences
- **Parser confusion** — some parsers may interpret control characters as delimiters
- **Security filter bypass** — WAFs may not inspect or sanitize non-printable bytes

## Verdicts

- **Pass** — `400` (rejected) or `2xx` with control characters stripped/cookie dropped
- **Fail** — `2xx` with control characters preserved in the response body

## Sources

- [RFC 6265 §4.1.1](https://www.rfc-editor.org/rfc/rfc6265#section-4.1.1) — cookie-octet definition
