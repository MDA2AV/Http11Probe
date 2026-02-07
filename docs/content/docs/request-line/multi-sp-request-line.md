---
title: "MULTI-SP-REQUEST-LINE"
description: "MULTI-SP-REQUEST-LINE test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `RFC9112-3-MULTI-SP-REQUEST-LINE` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3](https://www.rfc-editor.org/rfc/rfc9112#section-3) |
| **Requirement** | SHOULD |
| **Expected** | `400` or close |

## What it sends

A request-line with multiple spaces between components: `GET  /  HTTP/1.1` (double spaces).

## What the RFC says

The request-line grammar is `method SP request-target SP HTTP-version CRLF` where `SP` is exactly one space. Multiple spaces do not match this grammar, making the request-line invalid. Recipients SHOULD respond with 400.

## Why it matters

Some parsers are lenient and collapse multiple spaces. If a front-end collapses spaces but a back-end does not, they may parse the method, target, or version differently — leading to routing confusion or bypass.

## Sources

- [RFC 9112 Section 3 — Request Line](https://www.rfc-editor.org/rfc/rfc9112#section-3)
