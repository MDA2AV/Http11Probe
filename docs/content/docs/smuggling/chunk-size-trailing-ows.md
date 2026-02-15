---
title: "CHUNK-SIZE-TRAILING-OWS"
description: "SMUG-CHUNK-SIZE-TRAILING-OWS test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-SIZE-TRAILING-OWS` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | Implicit MUST (grammar violation) |
| **Expected** | `400` or close |

## What it sends

A chunked request with trailing whitespace after the chunk-size token.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
5 \r\n
hello\r\n
0\r\n
\r\n
```

## What the RFC says

> "chunk-size = 1*HEXDIG" -- RFC 9112 Section 7.1

Whitespace is not part of `HEXDIG`; trailing OWS in chunk-size is invalid.

## Why it matters

Some parsers trim this value while others reject it. Differential behavior can create request boundary disagreements.

## Sources

- [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
