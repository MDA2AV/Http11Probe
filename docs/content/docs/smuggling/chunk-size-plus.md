---
title: "CHUNK-SIZE-PLUS"
description: "SMUG-CHUNK-SIZE-PLUS test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-SIZE-PLUS` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | Implicit MUST (grammar violation) |
| **Expected** | `400` or close |

## What it sends

A chunked request where chunk-size is prefixed by `+`.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
+5\r\n
hello\r\n
0\r\n
\r\n
```

## What the RFC says

> "chunk-size = 1*HEXDIG" -- RFC 9112 Section 7.1

The plus sign is not a hexadecimal digit. The chunk-size token is invalid.

## Why it matters

Lenient numeric parsing (`+5`) in one component and strict parsing in another creates parser disagreement and desync opportunities.

## Sources

- [RFC 9112 §7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
