---
title: "CL-NON-NUMERIC"
description: "CL-NON-NUMERIC test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `RFC9112-6.1-CL-NON-NUMERIC` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1), [Section 6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

A request with a non-numeric `Content-Length` value, e.g., `Content-Length: abc`.

## What the RFC says

Content-Length is defined as `1*DIGIT`. A value containing non-digit characters does not match this grammar.

> “If a message is received without Transfer-Encoding and with an invalid Content-Length header field, then the message framing is invalid and the recipient **MUST** treat it as an unrecoverable error...”

“Unrecoverable error” means the server must reject — either with a 400 response or by closing the connection. It cannot attempt to parse the body.

## Sources

- [RFC 9110 Section 8.6 — Content-Length](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
- [RFC 9112 Section 6.3 — Message Body Length](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
