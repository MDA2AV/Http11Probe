---
title: "TE-DUPLICATE-HEADERS"
description: "TE-DUPLICATE-HEADERS test documentation"
weight: 24
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-DUPLICATE-HEADERS` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

Two TE headers (`chunked` and `identity`) plus Content-Length.

## What the RFC says

> When both TE and CL are present, TE takes priority — but two conflicting TE headers create ambiguity.

## Why it matters

Different servers may pick different TE header values, causing body length disagreements.

## Sources

- [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
