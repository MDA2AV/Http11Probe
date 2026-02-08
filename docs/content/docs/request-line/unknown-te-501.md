---
title: "UNKNOWN-TE-501"
description: "UNKNOWN-TE-501 test documentation"
weight: 8
---

| | |
|---|---|
| **Test ID** | `COMP-UNKNOWN-TE-501` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | SHOULD respond with 501 |
| **Expected** | `400`/`501` or close |

## What it sends

`Transfer-Encoding: gzip` without any Content-Length — an unknown transfer coding as the only framing.

## What the RFC says

> "A server that receives a request message with a transfer coding it does not understand SHOULD respond with 501 (Not Implemented)." — RFC 9112 Section 6.1

## Why it matters

When a server doesn't understand the transfer coding and there's no Content-Length fallback, it cannot determine the message body boundaries. Rejecting or responding with 501 is correct.

## Sources

- [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
