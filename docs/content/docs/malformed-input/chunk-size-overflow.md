---
title: "CHUNK-SIZE-OVERFLOW"
description: "CHUNK-SIZE-OVERFLOW test documentation"
weight: 16
---

| | |
|---|---|
| **Test ID** | `MAL-CHUNK-SIZE-OVERFLOW` |
| **Category** | Malformed Input |
| **Expected** | `400` or close |

## What it sends

A chunked request with a chunk size of `FFFFFFFFFFFFFFFF0` — a value exceeding the maximum 64-bit unsigned integer.

## Why it matters

Integer overflow in chunk size parsing can lead to incorrect body length calculation, buffer overflows, or server crashes. A robust server must detect overflow and reject the request.

## Sources

- RFC 9112 Section 7.1 — chunk-size = 1*HEXDIG
