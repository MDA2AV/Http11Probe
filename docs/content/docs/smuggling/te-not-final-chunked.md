---
title: "TE-NOT-FINAL-CHUNKED"
description: "TE-NOT-FINAL-CHUNKED test documentation"
weight: 16
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-NOT-FINAL-CHUNKED` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 Section 7](https://www.rfc-editor.org/rfc/rfc9112#section-7) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

`Transfer-Encoding: chunked, gzip` — chunked is not the final encoding.

## What the RFC says

> "If any transfer coding other than chunked is applied to a request payload body, the sender MUST apply chunked as the final transfer coding." — RFC 9112 §7

## Why it matters

If chunked isn't final, the server cannot determine body boundaries. This can be exploited for smuggling.

## Sources

- [RFC 9112 Section 7](https://www.rfc-editor.org/rfc/rfc9112#section-7)
