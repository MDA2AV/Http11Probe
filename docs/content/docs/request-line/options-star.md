---
title: "OPTIONS-STAR"
description: "OPTIONS-STAR test documentation"
weight: 7
---

| | |
|---|---|
| **Test ID** | `COMP-OPTIONS-STAR` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2.4](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.4) |
| **Requirement** | MUST accept |
| **Expected** | `2xx` |

## What it sends

`OPTIONS * HTTP/1.1` — the valid asterisk-form request.

## What the RFC says

> "The asterisk-form of request-target is only used for a server-wide OPTIONS request." — RFC 9112 Section 3.2.4

## Why it matters

This is the only valid use of `*` as a request-target. A compliant server should accept it and respond with 2xx (typically 200 with Allow header).

## Sources

- [RFC 9112 Section 3.2.4](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.4)
- [RFC 9110 Section 9.3.7](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.7)
