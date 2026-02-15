---
title: "SPACE-IN-TARGET"
description: "SPACE-IN-TARGET test documentation"
weight: 32
---

| | |
|---|---|
| **Test ID** | `COMP-SPACE-IN-TARGET` |
| **Category** | Compliance |
| **Scored** | Yes |
| **RFC** | [RFC 9112 §3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) |
| **RFC Level** | MUST |
| **Expected** | `400` or connection close |

## What it sends

A GET request with an unencoded space inside the request-target.

```http
GET /pa th HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

The request-target `/pa th` contains a bare space character (0x20) which is not a valid URI character.

## What the RFC says

> "Recipients of an invalid request-line SHOULD respond with either a 400 (Bad Request) error or a 301 (Moved Permanently) redirect with the request-target properly encoded." — RFC 9112 §3.2

> "request-target = origin-form / absolute-form / authority-form / asterisk-form" — RFC 9112 §3.2

The space character is the delimiter between the method, request-target, and HTTP-version in the request-line. An unencoded space in the target makes the request-line ambiguous — the parser sees `GET /pa th HTTP/1.1` as having four tokens instead of three.

## Why it matters

A server that accepts a bare space in the request-target must be performing heuristic parsing to guess where the target ends. This ambiguity is a classic source of request smuggling and cache poisoning vulnerabilities, where different parsers in a chain disagree on the boundaries of the request-line.

## Verdicts

- **Pass** — Server rejects with `400` or closes the connection
- **Fail** — Server accepts the request

## Sources

- [RFC 9112 §3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2)
