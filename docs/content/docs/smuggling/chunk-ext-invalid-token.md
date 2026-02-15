---
title: "CHUNK-EXT-INVALID-TOKEN"
description: "SMUG-CHUNK-EXT-INVALID-TOKEN test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-EXT-INVALID-TOKEN` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1) |
| **Requirement** | Implicit MUST (grammar violation) |
| **Expected** | `400` or close |

## What it sends

A chunk extension with an invalid token character in the extension name (`bad[`):

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
5;bad[=x\r\n
hello\r\n
0\r\n
\r\n
```

## What the RFC says

> "chunk-ext-name = token" -- RFC 9112 Section 7.1.1

`[` is not a valid token character, so the extension syntax is invalid.

## Partial Coverage Note

Existing tests already cover malformed chunk extensions (`SMUG-CHUNK-BARE-SEMICOLON`, `SMUG-CHUNK-EXT-CTRL`, `SMUG-CHUNK-EXT-CR`, `SMUG-CHUNK-EXT-LF`). This case specifically targets invalid token characters in extension names.

## Why it matters

Different extension parsers may tokenize this differently, creating front-end/back-end framing inconsistencies.

## Sources

- [RFC 9112 §7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1)
