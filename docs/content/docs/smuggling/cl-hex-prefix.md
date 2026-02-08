---
title: "CL-HEX-PREFIX"
description: "CL-HEX-PREFIX test documentation"
weight: 26
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-HEX-PREFIX` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

`Content-Length: 0x5` — CL with hex prefix.

## What the RFC says

> Content-Length grammar is `1*DIGIT`. Hex notation is not valid.

## Why it matters

If a server parses `0x5` as 5, it could create body length disagreements with servers that reject it.

## Sources

- [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
