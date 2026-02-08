---
title: "CL-OCTAL"
description: "CL-OCTAL test documentation"
weight: 20
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-OCTAL` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

`Content-Length: 0o5` — CL with octal prefix.

## What the RFC says

> Content-Length grammar is `1*DIGIT`. The prefix `0o` makes it non-numeric.

## Why it matters

Some programming languages parse `0o5` as octal 5. If a server accepts this, attackers could create body length disagreements.

## Sources

- [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
