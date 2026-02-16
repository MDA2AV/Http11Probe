---
title: "EMPTY"
description: "COOK-EMPTY test documentation"
weight: 3
---

| | |
|---|---|
| **Test ID** | `COOK-EMPTY` |
| **Category** | Cookies |
| **Scored** | No |
| **Expected** | `2xx` or `400` |

## What it sends

```http
GET /echo HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: \r\n
\r\n
```

A `Cookie` header with an empty value (nothing after the colon and space).

## What the RFC says

> "cookie-header = 'Cookie:' OWS cookie-string OWS" — RFC 6265 §4.2

An empty cookie-string does not match `cookie-pair *( ";" SP cookie-pair )` since `cookie-pair` requires at least a name. However, servers should handle this gracefully.

## Why it matters

Empty `Cookie` headers can trigger null-pointer dereferences or empty-string edge cases in cookie parsers. The test verifies the server doesn't crash.

## Verdicts

- **Pass** — `2xx` (accepted) or `400` (rejected gracefully)
- **Fail** — `500` or connection crash

## Sources

- [RFC 6265 §4.2](https://www.rfc-editor.org/rfc/rfc6265#section-4.2) — cookie header syntax
