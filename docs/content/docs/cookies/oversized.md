---
title: "OVERSIZED"
description: "COOK-OVERSIZED cookie test documentation"
weight: 2
---

| | |
|---|---|
| **Test ID** | `COOK-OVERSIZED` |
| **Category** | Cookies |
| **Scored** | No |
| **RFC Level** | N/A |
| **Expected** | `400/431 (rejected) or 2xx (survived)` |

## What it sends

64KB Cookie header — tests header size limits on cookie data.

```http
GET /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: big=AAAA...AAAA\r\n
\r\n
```

The cookie value contains 65,536 bytes of `A`.

## Why it matters

Oversized cookies can trigger buffer overflows, OOM crashes, or excessive memory allocation in parsers that don't enforce size limits.

## Verdicts

- **Pass** — 400/431 rejected, or 2xx survived, or connection close
- **Fail** — 500 (crash)

## Sources

- [RFC 6265 §5.4](https://www.rfc-editor.org/rfc/rfc6265#section-5.4) — Cookie header
