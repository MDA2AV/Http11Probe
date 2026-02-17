---
title: "PARSED-SPECIAL"
description: "COOK-PARSED-SPECIAL cookie test documentation"
weight: 12
---

| | |
|---|---|
| **Test ID** | `COOK-PARSED-SPECIAL` |
| **Category** | Cookies |
| **Scored** | No |
| **RFC Level** | N/A |
| **Expected** | `2xx (no crash)` |

## What it sends

Cookies with spaces and = in values — tests framework parser edge cases.

```http
GET /cookie HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: a=hello world; b=x=y\r\n
\r\n
```

## Why it matters

Spaces in values and `=` signs within values are common in real-world cookies (e.g., Base64-encoded tokens) and can confuse parsers that split on `=` or whitespace too aggressively.

## Verdicts

- **Pass** — 2xx or 400
- **Warn** — 404 (endpoint not available)
- **Fail** — 500 (crash)

## Sources

- [RFC 6265 §5.4](https://www.rfc-editor.org/rfc/rfc6265#section-5.4) — Cookie header
