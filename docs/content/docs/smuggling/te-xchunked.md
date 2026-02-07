---
title: "TE-XCHUNKED"
weight: 5
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-XCHUNKED` |
| **Category** | Smuggling |
| **Expected** | `400` or close |

## What it sends

`Transfer-Encoding: xchunked` with a Content-Length header. The TE value `xchunked` is not a recognized encoding.

## Why it matters

If the front-end ignores the unknown TE and uses CL, but the back-end strips the `x` and processes it as `chunked`, a smuggling vector exists. Some real-world proxies have exhibited this exact behavior.

## Sources

- [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
