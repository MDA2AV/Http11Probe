---
title: "PARSED-MULTI"
description: "COOK-PARSED-MULTI cookie test documentation"
weight: 10
---

| | |
|---|---|
| **Test ID** | `COOK-PARSED-MULTI` |
| **Category** | Cookies |
| **Scored** | No |
| **RFC Level** | N/A |
| **Expected** | `2xx with a=1, b=2, c=3 in body` |

## What it sends

Multiple cookies parsed correctly by framework.

```http
GET /cookie HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: a=1; b=2; c=3\r\n
\r\n
```

## Why it matters

Tests the framework's ability to correctly split and parse multiple semicolon-delimited cookie pairs.

## Verdicts

- **Pass** — 2xx and body contains all three pairs
- **Warn** — 404 (endpoint not available)
- **Fail** — Missing pairs or 500

## Sources

- [RFC 6265 §5.4](https://www.rfc-editor.org/rfc/rfc6265#section-5.4) — Cookie header
