---
title: "CHUNKED-MULTI"
description: "CHUNKED-MULTI test documentation"
weight: 7
---

| | |
|---|---|
| **Test ID** | `COMP-CHUNKED-MULTI` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MUST accept |
| **Expected** | `2xx` |

## What it sends

A valid chunked POST with two data chunks (5 bytes + 6 bytes) followed by the zero terminator.

```http
POST / HTTP/1.1\r\n
Host: localhost\r\n
Transfer-Encoding: chunked\r\n
\r\n
5\r\n
hello\r\n
6\r\n
 world\r\n
0\r\n
\r\n
```

## What the RFC says

> "The chunked transfer coding wraps the payload body in order to transfer it as a series of chunks, each with its own size indicator." — RFC 9112 Section 7.1

The server must concatenate all chunks to reconstruct the full body. This tests that the chunk parser correctly handles multiple consecutive data chunks before the terminator.

## Why it matters

Multi-chunk bodies are the norm in real-world HTTP — streaming uploads, large form submissions, and proxied requests all use multiple chunks. A server that only handles single-chunk bodies has an incomplete chunked decoder.

## Sources

- [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
