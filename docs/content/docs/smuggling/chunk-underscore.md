---
title: "CHUNK-UNDERSCORE"
description: "CHUNK-UNDERSCORE test documentation"
weight: 21
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-UNDERSCORE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

Chunk size `1_0` — with underscore separator.

## What the RFC says

> Chunk size is `1*HEXDIG`. Underscores are not hex digits.

## Why it matters

Some language parsers accept `_` in numeric literals. If a server parses `1_0` as 10, it reads more data than intended.

## Sources

- [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
