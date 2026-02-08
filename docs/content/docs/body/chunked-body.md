---
title: "CHUNKED-BODY"
description: "CHUNKED-BODY test documentation"
weight: 6
---

| | |
|---|---|
| **Test ID** | `COMP-CHUNKED-BODY` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MUST accept |
| **Expected** | `2xx` |

## What it sends

A valid chunked POST with a single 5-byte chunk followed by the zero terminator.

```http
POST / HTTP/1.1\r\n
Host: localhost\r\n
Transfer-Encoding: chunked\r\n
\r\n
5\r\n
hello\r\n
0\r\n
\r\n
```

## What the RFC says

> "The chunked transfer coding wraps the payload body in order to transfer it as a series of chunks, each with its own size indicator, followed by an OPTIONAL trailer section containing trailer fields." — RFC 9112 Section 7.1

A server that supports HTTP/1.1 must be able to decode chunked transfer encoding.

## Why it matters

Chunked encoding is fundamental to HTTP/1.1 — it enables streaming, server-sent data, and requests where the body size isn't known in advance. If a server can't decode a basic chunked body, it cannot fully participate in HTTP/1.1.

## Sources

- [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
