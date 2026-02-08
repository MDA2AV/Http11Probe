---
title: "CHUNKED-EXTENSION"
description: "CHUNKED-EXTENSION test documentation"
weight: 10
---

| | |
|---|---|
| **Test ID** | `COMP-CHUNKED-EXTENSION` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1) |
| **Requirement** | SHOULD accept |
| **Expected** | `2xx` = Pass; `400` = Warn |

## What it sends

A chunked POST where the chunk size line includes a valid extension: `5;ext=value`.

```http
POST / HTTP/1.1\r\n
Host: localhost\r\n
Transfer-Encoding: chunked\r\n
\r\n
5;ext=value\r\n
hello\r\n
0\r\n
\r\n
```

## What the RFC says

> "The chunked encoding allows each chunk to include zero or more chunk extensions, immediately following the chunk-size, for the sake of supplying per-chunk metadata." — RFC 9112 Section 7.1.1

Chunk extensions are part of the chunked encoding grammar. A compliant parser should skip unrecognized extensions and process the chunk data normally.

## Why it matters

While chunk extensions are rarely used in practice, they are syntactically valid. A server that rejects them has an overly strict chunk parser that may break with legitimate clients or proxies that add extensions for metadata (e.g., checksums, signatures).

## Sources

- [RFC 9112 Section 7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1)
