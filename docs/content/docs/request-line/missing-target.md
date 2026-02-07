---
title: "MISSING-TARGET"
weight: 2
---

| | |
|---|---|
| **Test ID** | `RFC9112-3-MISSING-TARGET` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3](https://www.rfc-editor.org/rfc/rfc9112#section-3) |
| **Requirement** | SHOULD |
| **Expected** | `400` or close |

## What it sends

A request-line with no request-target: `GET  HTTP/1.1` (method directly followed by version, no URI).

## What the RFC says

The request-target is a required component of the request-line grammar. Without it, the line is invalid. Recipients SHOULD respond with 400.

## Sources

- [RFC 9112 Section 3 — Request Line](https://www.rfc-editor.org/rfc/rfc9112#section-3)
