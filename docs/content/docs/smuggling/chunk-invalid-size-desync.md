---
title: "CHUNK-INVALID-SIZE-DESYNC"
description: "SMUG-CHUNK-INVALID-SIZE-DESYNC test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-INVALID-SIZE-DESYNC` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

A two-step sequence: invalid chunk-size `+0` with poison byte `X`, then a clean `GET`.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
+0\r\n
\r\n
X

GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "chunk-size = 1*HEXDIG" -- RFC 9112 Section 7.1

Invalid chunk-size is a framing error. This sequence confirms whether acceptance leads to follow-up parsing corruption.

## Partial Coverage Note

Existing tests (`SMUG-CHUNK-NEGATIVE`, `SMUG-CHUNK-HEX-PREFIX`, `SMUG-CHUNK-SPILL`, `MAL-CHUNK-SIZE-OVERFLOW`) cover invalid chunk primitives. This test adds explicit desync confirmation via a follow-up request.

## Why it matters

If invalid chunk-size is tolerated and the connection remains open, poison bytes can be interpreted as the next request.

## Sources

- [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
