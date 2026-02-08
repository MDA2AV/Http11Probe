---
title: "CHUNK-HEX-PREFIX"
description: "CHUNK-HEX-PREFIX test documentation"
weight: 25
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-HEX-PREFIX` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

Chunk size `0x5` — with C-style hex prefix.

## What the RFC says

> Chunk size is `1*HEXDIG` — no prefix notation allowed.

## Why it matters

`0x5` is valid hex in C but invalid in HTTP chunked encoding (the `0x` prefix is not HEXDIG).

## Sources

- [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
