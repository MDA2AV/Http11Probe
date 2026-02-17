---
title: "MANY-PAIRS"
description: "COOK-MANY-PAIRS cookie test documentation"
weight: 6
---

| | |
|---|---|
| **Test ID** | `COOK-MANY-PAIRS` |
| **Category** | Cookies |
| **Scored** | No |
| **RFC Level** | N/A |
| **Expected** | `2xx or 400/431` |

## What it sends

1000 cookie key=value pairs — tests parser performance limits.

```http
GET /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: k0=v0; k1=v1; ... k999=v999\r\n
\r\n
```

## Why it matters

A large number of cookie pairs can cause O(n^2) parsing behavior, hashtable flooding, or memory exhaustion in frameworks that eagerly parse all cookies.

## Verdicts

- **Pass** — 2xx or 400/431
- **Fail** — 500 (crash)

## Sources

- [RFC 6265 §5.4](https://www.rfc-editor.org/rfc/rfc6265#section-5.4) — Cookie header
