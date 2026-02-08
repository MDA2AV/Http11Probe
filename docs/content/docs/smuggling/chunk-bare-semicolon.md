---
title: "CHUNK-BARE-SEMICOLON"
description: "CHUNK-BARE-SEMICOLON test documentation"
weight: 18
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-BARE-SEMICOLON` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 Section 7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

Chunk size `5;` with a semicolon but no extension name.

## What the RFC says

> The chunk extension grammar requires `chunk-ext = *( BWS ";" BWS chunk-ext-name [ "=" chunk-ext-val ] )` where chunk-ext-name is a token (1 or more characters).

## Why it matters

A bare semicolon can cause parser confusion about chunk boundaries, potentially enabling smuggling.

## Sources

- [RFC 9112 Section 7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1)
