---
title: "CHUNKED-NO-FINAL"
description: "CHUNKED-NO-FINAL test documentation"
weight: 9
---

| | |
|---|---|
| **Test ID** | `COMP-CHUNKED-NO-FINAL` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MUST read until terminator |
| **Expected** | `400`, close, or timeout |

## What it sends

A chunked POST with one data chunk but no zero terminator. The connection then goes silent.

```http
POST / HTTP/1.1\r\n
Host: localhost\r\n
Transfer-Encoding: chunked\r\n
\r\n
5\r\n
hello\r\n
```

## What the RFC says

> "The last chunk has a chunk size of zero, indicating the end of the chunk data." — RFC 9112 Section 7.1

Without the `0\r\n\r\n` terminator, the transfer is incomplete. The server must continue waiting for more chunks until its read timeout fires.

## Why it matters

A server that responds before seeing the zero terminator risks connection desynchronization — subsequent requests on the same connection could be misinterpreted. This is analogous to the Content-Length undersend scenario but for chunked encoding.

## Sources

- [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
