---
title: "TE-LEADING-COMMA"
description: "TE-LEADING-COMMA test documentation"
weight: 23
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-LEADING-COMMA` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

`Transfer-Encoding: , chunked` — leading comma.

## What the RFC says

> The list syntax does not allow empty list members (leading comma).

## Why it matters

Some parsers strip leading commas and see "chunked", while others reject the value. This discrepancy enables smuggling.

## Sources

- [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
