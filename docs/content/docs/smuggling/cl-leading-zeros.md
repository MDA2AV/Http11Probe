---
title: "CL-LEADING-ZEROS"
weight: 3
---

| | |
|---|---|
| **Test ID** | `RFC9112-6.1-CL-LEADING-ZEROS` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Expected** | `400` or close |

## What it sends

Content-Length with leading zeros: `Content-Length: 007`.

## What the RFC says

While `007` matches `1*DIGIT`, leading zeros create ambiguity. Some parsers may interpret as octal, some as decimal.

## Sources

- [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
