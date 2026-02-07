---
title: "CL-PLUS-SIGN"
weight: 2
---

| | |
|---|---|
| **Test ID** | `RFC9112-6.1-CL-PLUS-SIGN` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1), [Section 6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3) |
| **Requirement** | MUST |
| **Expected** | `400` or close |

## What it sends

A request with a plus sign in the Content-Length value: `Content-Length: +42`.

## What the RFC says

The `+` character is not in the DIGIT set (`%x30-39`), so `+42` does not match `1*DIGIT`. This is an invalid Content-Length and MUST be treated as an unrecoverable error.

## Why it matters

Many programming languages’ integer parsers accept leading `+` signs (e.g., `parseInt("+42")` returns `42` in JavaScript). A server that blindly passes Content-Length through such a parser may accept this value while another server in the chain rejects it — creating a framing disagreement.

## Sources

- [RFC 9110 Section 8.6 — Content-Length](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
- [RFC 9112 Section 6.3 — Message Body Length](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
