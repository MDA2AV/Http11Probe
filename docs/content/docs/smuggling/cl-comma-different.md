---
title: "CL-COMMA-DIFFERENT"
description: "CL-COMMA-DIFFERENT test documentation"
weight: 15
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-COMMA-DIFFERENT` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

`Content-Length: 5, 10` — comma-separated CL with different values.

## What the RFC says

> "If a message is received without Transfer-Encoding and with either multiple Content-Length header fields having differing field-line values or a single Content-Length header field having an invalid value, then the message framing is invalid and the recipient MUST treat it as an unrecoverable error." — RFC 9112 §6.3

## Why it matters

Comma-separated CL values are equivalent to multiple CL headers. Different values create ambiguity about body length.

## Sources

- [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
