---
title: "TE-CASE-MISMATCH"
description: "TE-CASE-MISMATCH test documentation"
weight: 14
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-CASE-MISMATCH` |
| **Category** | Smuggling (Unscored) |
| **Expected** | `400` (strict) or `2xx` (RFC-compliant) |

## What it sends

`Transfer-Encoding: Chunked` — capital `C` instead of lowercase.

## Why it's unscored

Transfer coding names are case-insensitive per RFC 9112 Section 6.1. Recognizing `Chunked` as `chunked` is correct behavior. However, some parsers are case-sensitive, creating a potential desync.

## Sources

- [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
