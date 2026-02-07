---
title: "INVALID-VERSION"
description: "INVALID-VERSION test documentation"
weight: 4
---

| | |
|---|---|
| **Test ID** | `RFC9112-2.3-INVALID-VERSION` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 2.3](https://www.rfc-editor.org/rfc/rfc9112#section-2.3) |
| **Requirement** | No MUST |
| **Expected** | `400`, `505`, or close |

## What it sends

A request with an unrecognizable HTTP version string, e.g., `GET / HTTP/9.9`.

## What the RFC says

HTTP-version is defined as `HTTP-name "/" DIGIT "." DIGIT` and is case-sensitive. The 505 (HTTP Version Not Supported) status code is available but the RFC uses SHOULD, not MUST:

> "A server **SHOULD** respond with the 505 (HTTP Version Not Supported) status code" — RFC 9110 Section 15.6.6

There is no MUST-level requirement for a specific response to invalid versions.

## Sources

- [RFC 9112 Section 2.3 — HTTP Version](https://www.rfc-editor.org/rfc/rfc9112#section-2.3)
- [RFC 9110 Section 15.6.6 — 505 HTTP Version Not Supported](https://www.rfc-editor.org/rfc/rfc9110#section-15.6.6)
