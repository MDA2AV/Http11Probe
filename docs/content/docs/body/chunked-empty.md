---
title: "CHUNKED-EMPTY"
description: "CHUNKED-EMPTY test documentation"
weight: 8
---

| | |
|---|---|
| **Test ID** | `COMP-CHUNKED-EMPTY` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MUST accept |
| **Expected** | `2xx` or close |

## What it sends

A chunked POST with only the zero terminator — a zero-length body.

```http
POST / HTTP/1.1\r\n
Host: localhost\r\n
Transfer-Encoding: chunked\r\n
\r\n
0\r\n
\r\n
```

## What the RFC says

> "The last chunk has a chunk size of zero, indicating the end of the chunk data." — RFC 9112 Section 7.1

A zero-size first chunk is valid and indicates an empty body. The server must recognize the terminator and not block waiting for additional data.

## Why it matters

Empty chunked bodies occur when a client starts a chunked transfer but has nothing to send, or when a proxy rewrites a zero-length CL body into chunked encoding. The server must handle this edge case cleanly.

## Sources

- [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
