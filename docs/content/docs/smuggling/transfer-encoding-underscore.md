---
title: "TRANSFER_ENCODING"
description: "TRANSFER_ENCODING test documentation"
weight: 30
---

| | |
|---|---|
| **Test ID** | `SMUG-TRANSFER_ENCODING` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | Unscored |
| **Expected** | `400` or `2xx` |

## What it sends

`Transfer_Encoding: chunked` (underscore instead of hyphen) with `Content-Length: 5`.

## What the RFC says

> Header names are tokens. `Transfer_Encoding` is a valid token but not the `Transfer-Encoding` header.

## Why it matters

This is unscored. Some proxies normalize underscores to hyphens (notably certain Python/Ruby frameworks), making this a known smuggling vector. Both `400` and `2xx` are acceptable since it's technically a different header.

## Sources

- [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
