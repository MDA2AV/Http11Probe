---
title: "PARSED-MULTI"
description: "COOK-PARSED-MULTI test documentation"
weight: 10
---

| | |
|---|---|
| **Test ID** | `COOK-PARSED-MULTI` |
| **Category** | Cookies |
| **Scored** | No |
| **Expected** | `2xx` with `a=1`, `b=2`, `c=3` in body |

## What it sends

```http
GET /cookie HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: a=1; b=2; c=3\r\n
\r\n
```

A request with three cookies separated by `; ` (semicolon-space) in a single `Cookie` header.

## What the RFC says

> "cookie-string = cookie-pair *( ';' SP cookie-pair )" — RFC 6265 §4.2.1

Multiple cookies in a single header are delimited by `; ` (semicolon followed by a space). The parser must split on this delimiter and extract all pairs.

## Why it matters

Most real-world requests contain multiple cookies (session IDs, preferences, tracking tokens). If the framework parser fails to split on `; ` correctly, it will lose cookies — potentially dropping session tokens or authentication data.

## Verdicts

- **Pass** — `2xx` with all three cookies (`a=1`, `b=2`, `c=3`) in the response body
- **Warn** — `404` (endpoint not available)
- **Fail** — `2xx` with missing cookies, or `500`

## Sources

- [RFC 6265 §4.2.1](https://www.rfc-editor.org/rfc/rfc6265#section-4.2.1) — cookie-string syntax
