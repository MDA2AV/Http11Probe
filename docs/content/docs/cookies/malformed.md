---
title: "MALFORMED"
description: "COOK-MALFORMED test documentation"
weight: 7
---

| | |
|---|---|
| **Test ID** | `COOK-MALFORMED` |
| **Category** | Cookies |
| **Scored** | No |
| **Expected** | `2xx` or `400` |

## What it sends

```http
GET /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: ===;;;\r\n
\r\n
```

A `Cookie` header with completely invalid syntax — no valid cookie-name, only equals signs and semicolons.

## What the RFC says

> "cookie-pair = cookie-name '=' cookie-value" — RFC 6265 §4.1.1

> "cookie-name = token" — RFC 6265 §4.1.1

The value `===;;;` does not match the `cookie-pair` grammar. There is no valid `cookie-name` (an empty name before the first `=` is not a valid token).

## Why it matters

Framework cookie parsers must handle completely malformed cookie strings gracefully. This tests the worst-case scenario for parser resilience — the value bears no resemblance to valid cookie syntax.

## Verdicts

- **Pass** — `2xx` (survived) or `400` (rejected gracefully)
- **Fail** — `500` or connection crash

## Sources

- [RFC 6265 §4.1.1](https://www.rfc-editor.org/rfc/rfc6265#section-4.1.1) — cookie-pair syntax
