---
title: "TE-EMPTY-VALUE"
description: "TE-EMPTY-VALUE test documentation"
weight: 22
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-EMPTY-VALUE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

`Transfer-Encoding: ` (empty value) with `Content-Length: 5`.

## What the RFC says

> Transfer-Encoding must contain at least one valid transfer coding.

## Why it matters

An empty TE value creates ambiguity — should the server use CL or consider TE present?

## Sources

- [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
