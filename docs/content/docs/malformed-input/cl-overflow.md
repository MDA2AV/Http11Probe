---
title: "CL-OVERFLOW"
description: "CL-OVERFLOW test documentation"
weight: 11
---

| | |
|---|---|
| **Test ID** | `MAL-CL-OVERFLOW` |
| **Category** | Malformed Input |
| **Expected** | `400` or close |

## What it sends

A `Content-Length` value exceeding the 64-bit integer range (e.g., `99999999999999999999`).

## Why it matters

If a parser uses a fixed-width integer without overflow checking, the parsed value wraps around. This can lead to reading a different amount of body data than intended -- a smuggling vector.

## Sources

- [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
