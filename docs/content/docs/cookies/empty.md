---
title: "EMPTY"
description: "COOK-EMPTY cookie test documentation"
weight: 3
---

| | |
|---|---|
| **Test ID** | `COOK-EMPTY` |
| **Category** | Cookies |
| **Scored** | No |
| **RFC Level** | N/A |
| **Expected** | `2xx or 400` |

## What it sends

Empty Cookie header value — tests parser resilience.

```http
GET /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: \r\n
\r\n
```

## Why it matters

Empty Cookie headers can cause null-reference exceptions or crashes in parsers that assume at least one key=value pair.

## Verdicts

- **Pass** — 2xx or 400
- **Fail** — 500 (crash)

## Sources

- [RFC 6265 §5.4](https://www.rfc-editor.org/rfc/rfc6265#section-5.4) — Cookie header
