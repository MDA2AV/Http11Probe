---
title: "MULTI-HEADER"
description: "COOK-MULTI-HEADER cookie test documentation"
weight: 8
---

| | |
|---|---|
| **Test ID** | `COOK-MULTI-HEADER` |
| **Category** | Cookies |
| **Scored** | No |
| **RFC Level** | N/A |
| **Expected** | `2xx with both cookies` |

## What it sends

Two separate Cookie headers — should be folded per RFC 6265 §5.4.

```http
GET /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: a=1\r\n
Cookie: b=2\r\n
\r\n
```

## Why it matters

RFC 6265 §5.4 says the user agent SHOULD combine multiple cookie values with `; `, but servers must handle receiving them separately since some clients and proxies split them.

## Verdicts

- **Pass** — 2xx with both a=1 and b=2 in body
- **Warn** — Only one cookie echoed, or 400
- **Fail** — 500 (crash)

## Sources

- [RFC 6265 §5.4](https://www.rfc-editor.org/rfc/rfc6265#section-5.4) — Cookie header
