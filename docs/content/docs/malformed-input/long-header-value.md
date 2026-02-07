---
title: "LONG-HEADER-VALUE"
weight: 4
---

| | |
|---|---|
| **Test ID** | `MAL-LONG-HEADER-VALUE` |
| **Category** | Malformed Input |
| **Expected** | `400`, `431`, or close |

## What it sends

A request with a ~100 KB header field value.

## Sources

- [RFC 6585 Section 5](https://www.rfc-editor.org/rfc/rfc6585#section-5)
