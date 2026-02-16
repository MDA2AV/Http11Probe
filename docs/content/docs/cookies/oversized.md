---
title: "OVERSIZED"
description: "COOK-OVERSIZED test documentation"
weight: 2
---

| | |
|---|---|
| **Test ID** | `COOK-OVERSIZED` |
| **Category** | Cookies |
| **Scored** | No |
| **Expected** | `400`/`431` (rejected) or `2xx` (survived) |

## What it sends

```http
GET /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: big=AAAA...AAAA\r\n
\r\n
```

A `Cookie` header with a 64KB value (65,536 `A` characters).

## What the RFC says

> "Practical cookie limits: At least 4096 bytes per cookie (as measured by the sum of the length of the cookie's name, value, and attributes)." — RFC 6265 §6.1

64KB vastly exceeds the recommended 4096-byte minimum. Servers are free to reject oversized cookies.

> "The 431 status code indicates that the server is unwilling to process the request because its header fields are too large." — RFC 6585 §5

## Why it matters

Oversized cookie headers can exhaust server memory or trigger buffer overflows in cookie parsers. A well-behaved server should either reject the request (400/431) or accept it without crashing.

## Verdicts

- **Pass** — `400`/`431` (rejected) or `2xx` (survived without crash)
- **Fail** — `500` or connection crash

## Sources

- [RFC 6265 §6.1](https://www.rfc-editor.org/rfc/rfc6265#section-6.1) — cookie limits
- [RFC 6585 §5](https://www.rfc-editor.org/rfc/rfc6585#section-5) — 431 status code
