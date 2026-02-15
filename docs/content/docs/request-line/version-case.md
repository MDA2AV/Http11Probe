---
title: "VERSION-CASE"
description: "VERSION-CASE test documentation"
weight: 30
---

| | |
|---|---|
| **Test ID** | `COMP-VERSION-CASE` |
| **Category** | Compliance |
| **Scored** | Yes |
| **RFC** | [RFC 9112 §2.3](https://www.rfc-editor.org/rfc/rfc9112#section-2.3) |
| **RFC Level** | MUST |
| **Expected** | `400` or connection close |

## What it sends

A GET request with lowercase `http/1.1` instead of `HTTP/1.1`.

```http
GET / http/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "HTTP-version is case-sensitive." — RFC 9112 §2.3

> "HTTP-version = HTTP-name '/' DIGIT '.' DIGIT" — RFC 9112 §2.3

> "HTTP-name = %x48.54.54.50 ; 'HTTP'" — RFC 9112 §2.3

The ABNF specifies the exact octets `H`, `T`, `T`, `P` — only uppercase matches.

## Why it matters

A server that accepts `http/1.1` as valid is performing case-insensitive comparison on the HTTP version, which violates the protocol specification. While unlikely to cause security issues on its own, lenient parsing of protocol-level tokens can mask deeper parsing inconsistencies that smuggling attacks exploit.

## Verdicts

- **Pass** — Server rejects with `400` or closes the connection
- **Fail** — Server accepts the request

## Sources

- [RFC 9112 §2.3](https://www.rfc-editor.org/rfc/rfc9112#section-2.3)
