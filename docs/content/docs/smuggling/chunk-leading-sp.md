---
title: "CHUNK-LEADING-SP"
description: "CHUNK-LEADING-SP test documentation"
weight: 28
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-LEADING-SP` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

Chunk size ` 5` — with leading space.

## What the RFC says

> Chunk size is `1*HEXDIG` with no leading whitespace.

## Why it matters

Leading whitespace in chunk sizes can cause parser disagreements about chunk boundaries.

## Sources

- [RFC 9112 Section 7.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1)
