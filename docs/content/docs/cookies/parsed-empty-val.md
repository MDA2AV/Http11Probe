---
title: "PARSED-EMPTY-VAL"
description: "COOK-PARSED-EMPTY-VAL test documentation"
weight: 11
---

| | |
|---|---|
| **Test ID** | `COOK-PARSED-EMPTY-VAL` |
| **Category** | Cookies |
| **Scored** | No |
| **Expected** | `2xx` (no crash) |

## What it sends

```http
GET /cookie HTTP/1.1\r\n
Host: localhost:8080\r\n
Cookie: foo=\r\n
\r\n
```

A cookie with an empty value — the key `foo` is present but its value is an empty string.

## What the RFC says

> "cookie-value = *cookie-octet / ( DQUOTE *cookie-octet DQUOTE )" — RFC 6265 §4.1.1

The `*` (zero or more) operator means an empty cookie-value is syntactically valid. The parser should accept `foo=` and store `foo` with an empty string value.

## Why it matters

Empty cookie values are common in practice — they often represent cleared or expired cookies. A parser that crashes on empty values has a serious resilience issue.

## Verdicts

- **Pass** — `2xx` (with or without `foo=` in the body — survival is the key)
- **Warn** — `404` (endpoint not available)
- **Fail** — `500` or connection crash

## Sources

- [RFC 6265 §4.1.1](https://www.rfc-editor.org/rfc/rfc6265#section-4.1.1) — cookie-value syntax
