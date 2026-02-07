---
title: "DUPLICATE-CL"
weight: 2
---

| | |
|---|---|
| **Test ID** | `RFC9110-8.6-DUPLICATE-CL` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

Two `Content-Length` headers with different values.

## What the RFC says

> "If a message is received without Transfer-Encoding and with either multiple Content-Length header field values... the recipient **MUST** treat it as an unrecoverable error."

## Why it matters

If parser A uses the first CL and parser B uses the second, they disagree on body length — classic smuggling.

## Sources

- [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
- [RFC 9112 Section 6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
