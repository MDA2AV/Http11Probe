---
title: "FRAGMENT-IN-TARGET"
description: "FRAGMENT-IN-TARGET test documentation"
weight: 3
---

| | |
|---|---|
| **Test ID** | `RFC9112-3.2-FRAGMENT-IN-TARGET` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) |
| **Requirement** | SHOULD |
| **Expected** | `400` or close |

## What it sends

A request with a fragment identifier in the URI: `GET /path#fragment HTTP/1.1`.

## What the RFC says

The origin-form of request-target is `absolute-path [ "?" query ]`. There is no fragment component. A fragment identifier (`#...`) does not appear in any valid request-target form.

> "Recipients of an invalid request-line **SHOULD** respond with either a 400 (Bad Request) error..." — RFC 9112 Section 3

## Why it matters

Fragments are a client-side concept (they reference a position within a document). They should never appear on the wire. A server that silently strips fragments may process a different resource than what the client intended.

## Sources

- [RFC 9112 Section 3.2 — origin-form](https://www.rfc-editor.org/rfc/rfc9112#section-3.2)
- [RFC 9110 Section 4.1 — URI References](https://www.rfc-editor.org/rfc/rfc9110#section-4.1)
