---
title: "CL-NEGATIVE"
weight: 4
---

| | |
|---|---|
| **Test ID** | `RFC9112-6.1-CL-NEGATIVE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

Negative Content-Length: `Content-Length: -1`.

## What the RFC says

`-` is not a digit. Invalid Content-Length MUST be treated as an unrecoverable error.

## Sources

- [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
- [RFC 9112 Section 6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
