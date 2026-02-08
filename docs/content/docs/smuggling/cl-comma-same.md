---
title: "CL-COMMA-SAME"
description: "CL-COMMA-SAME test documentation"
weight: 31
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-COMMA-SAME` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | Unscored |
| **Expected** | `400` or `2xx` |

## What it sends

`Content-Length: 5, 5` — comma-separated CL with identical values.

## What the RFC says

> "If the different field line values all have the same value... the recipient MAY accept that value." — RFC 9110 §8.6

## Why it matters

This is unscored. The RFC explicitly allows merging identical CL values. Both `400` (strict) and `2xx` (permissive) are RFC-compliant.

## Sources

- [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
