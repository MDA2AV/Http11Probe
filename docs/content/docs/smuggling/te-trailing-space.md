---
title: "TE-TRAILING-SPACE"
description: "TE-TRAILING-SPACE test documentation"
weight: 6
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-TRAILING-SPACE` |
| **Category** | Smuggling |
| **Expected** | `400` or close |

## What it sends

`Transfer-Encoding: chunked ` (with a trailing space). The value does not exactly match `chunked`.

## Why it matters

If one parser trims whitespace and recognizes `chunked` while another treats `chunked ` as an unknown encoding, they'll disagree on body framing.

## Sources

- [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
