---
title: "TE-DOUBLE-CHUNKED"
description: "TE-DOUBLE-CHUNKED test documentation"
weight: 13
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-DOUBLE-CHUNKED` |
| **Category** | Smuggling (Unscored) |
| **Expected** | `400` (strict) or `2xx` (RFC-compliant) |

## What it sends

`Transfer-Encoding: chunked, chunked` — duplicate `chunked` encoding with a Content-Length header also present.

## Why it's unscored

The RFC allows multiple Transfer-Encoding values. However, applying `chunked` twice is unusual and may confuse parsers. Both `400` and normal processing are defensible.

## Sources

- [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
