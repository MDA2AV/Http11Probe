---
title: "CLTE-PIPELINE"
description: "CLTE-PIPELINE test documentation"
weight: 8
---

| | |
|---|---|
| **Test ID** | `SMUG-CLTE-PIPELINE` |
| **Category** | Smuggling |
| **Expected** | `400` or close |

## What it sends

A full CL.TE smuggling payload — a POST request with both Content-Length and Transfer-Encoding headers, where the body contains a chunked `0` terminator followed by a smuggled second request.

## Why it matters

This is not a theoretical test — it's the actual attack payload. If the server processes the first request using CL and the second appears in the pipeline, the smuggling succeeded.

## Sources

- [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [PortSwigger — HTTP Request Smuggling](https://portswigger.net/web-security/request-smuggling)
