---
title: "PARSED-EMPTY-VAL"
description: "COOK-PARSED-EMPTY-VAL cookie test documentation"
weight: 11
---

| | |
|---|---|
| **Test ID** | `COOK-PARSED-EMPTY-VAL` |
| **Category** | Cookies |
| **Scored** | No |
| **RFC Level** | N/A |
| **Expected** | `2xx (no crash)` |

## What it sends

Cookie with empty value parsed without crash.

```http
GET /cookie HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: foo=\r\n
\r\n
```

## Why it matters

Cookies with empty values (`foo=`) are valid per RFC 6265 but can crash parsers that assume a non-empty value after the `=` sign.

## Verdicts

- **Pass** — 2xx or 400
- **Warn** — 404 (endpoint not available)
- **Fail** — 500 (crash)

## Sources

- [RFC 6265 §5.4](https://www.rfc-editor.org/rfc/rfc6265#section-5.4) — Cookie header
