---
title: "LONG-URL-OK"
description: "LONG-URL-OK test documentation"
weight: 31
---

| | |
|---|---|
| **Test ID** | `COMP-LONG-URL-OK` |
| **Category** | Compliance |
| **Scored** | Yes |
| **RFC** | [RFC 9112 §3](https://www.rfc-editor.org/rfc/rfc9112#section-3) |
| **RFC Level** | SHOULD |
| **Expected** | Any status except `414` |

## What it sends

A GET request with a ~7900-character path (well under 8000 octets total for the request-line).

```http
GET /aaaa...aaa HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

The path contains 7900 repetitions of `a`.

## What the RFC says

> "A server that receives a request-target longer than any URI it wishes to parse MUST respond with a 414 (URI Too Long) status code." — RFC 9112 §3

> "It is RECOMMENDED that all HTTP senders and recipients support, at a minimum, request-line lengths of 8000 octets." — RFC 9112 §3

## Why it matters

Servers that reject URLs well within the 8000-octet recommendation may break legitimate applications that use long query strings or path parameters. This test verifies the server can handle a request-line just under the recommended minimum.

This is the inverse of `MAL-LONG-URL`, which tests rejection of extremely long URLs (~100KB). Together they verify a server has reasonable upper and lower bounds.

## Verdicts

- **Pass** — Server returns any status other than `414`
- **Fail** — Server returns `414 URI Too Long`
- **Warn** — Server closes the connection without a response

## Sources

- [RFC 9112 §3](https://www.rfc-editor.org/rfc/rfc9112#section-3)
