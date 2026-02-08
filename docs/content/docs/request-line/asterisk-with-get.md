---
title: "ASTERISK-WITH-GET"
description: "ASTERISK-WITH-GET test documentation"
weight: 6
---

| | |
|---|---|
| **Test ID** | `COMP-ASTERISK-WITH-GET` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2.4](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.4) |
| **Requirement** | MUST only be used with OPTIONS |
| **Expected** | `400` or close |

## What it sends

`GET * HTTP/1.1` — the asterisk-form request-target with a non-OPTIONS method.

## What the RFC says

> "The asterisk-form of request-target is only used for a server-wide OPTIONS request." — RFC 9112 Section 3.2.4

## Why it matters

Asterisk-form with any method other than OPTIONS is invalid. Accepting it could lead to unexpected server behavior.

## Sources

- [RFC 9112 Section 3.2.4](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.4)
