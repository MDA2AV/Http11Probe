---
title: "INVALID-HEADER-NAME"
description: "INVALID-HEADER-NAME test documentation"
weight: 4
---

| | |
|---|---|
| **Test ID** | `RFC9112-5-INVALID-HEADER-NAME` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 5](https://www.rfc-editor.org/rfc/rfc9112#section-5) |
| **Requirement** | Implicit MUST (grammar violation) |
| **Expected** | `400` or close |

## What it sends

A header with non-token characters in the field name (e.g., characters outside the `tchar` set defined in RFC 9110 Section 5.6.2).

## What the RFC says

The `tchar` set is: `"!" / "#" / "$" / "%" / "&" / "'" / "*" / "+" / "-" / "." / "^" / "_" / "`" / "|" / "~" / DIGIT / ALPHA`. Characters outside this set in a field name violate the grammar.

## Sources

- [RFC 9112 Section 5 — Field Syntax](https://www.rfc-editor.org/rfc/rfc9112#section-5)
- [RFC 9110 Section 5.6.2 — Tokens](https://www.rfc-editor.org/rfc/rfc9110#section-5.6.2)
