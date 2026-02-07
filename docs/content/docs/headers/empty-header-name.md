---
title: "EMPTY-HEADER-NAME"
description: "EMPTY-HEADER-NAME test documentation"
weight: 3
---

| | |
|---|---|
| **Test ID** | `RFC9112-5-EMPTY-HEADER-NAME` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 5](https://www.rfc-editor.org/rfc/rfc9112#section-5) |
| **Requirement** | Implicit MUST (grammar violation) |
| **Expected** | `400` or close |

## What it sends

A header line starting with a colon — effectively an empty field name: `: value`.

## What the RFC says

Field names are defined as `token = 1*tchar`, requiring at least one valid token character. An empty string does not match `1*tchar`. While there is no explicit "MUST reject empty field names with 400" statement, a line starting with `:` fails to match the `field-line` grammar entirely.

## Sources

- [RFC 9112 Section 5 — Field Syntax](https://www.rfc-editor.org/rfc/rfc9112#section-5)
- [RFC 9110 Section 5.1 — Field Names](https://www.rfc-editor.org/rfc/rfc9110#section-5.1)
