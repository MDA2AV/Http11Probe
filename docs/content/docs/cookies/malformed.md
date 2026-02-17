---
title: "MALFORMED"
description: "COOK-MALFORMED cookie test documentation"
weight: 7
---

| | |
|---|---|
| **Test ID** | `COOK-MALFORMED` |
| **Category** | Cookies |
| **Scored** | No |
| **RFC Level** | N/A |
| **Expected** | `2xx or 400` |

## What it sends

Completely malformed cookie value (===;;;) — tests parser crash resilience.

```http
GET /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: ===;;;\r\n
\r\n
```

## Why it matters

Garbage cookie values with no valid key=value structure can crash naive parsers that split on `=` without bounds checking.

## Verdicts

- **Pass** — 2xx or 400
- **Fail** — 500 (crash)

## Sources

- [RFC 6265 §5.4](https://www.rfc-editor.org/rfc/rfc6265#section-5.4) — Cookie header
