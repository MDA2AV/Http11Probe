---
title: "CL-TRAILING-SPACE"
description: "CL-TRAILING-SPACE test documentation"
weight: 10
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-TRAILING-SPACE` |
| **Category** | Smuggling (Unscored) |
| **Expected** | `400` (strict) or `2xx` (RFC-compliant) |

## What it sends

`Content-Length: 0 ` — trailing space after the value.

## Why it's unscored

OWS (optional whitespace) after the field value is explicitly permitted by RFC 9112 Section 5. Trimming it and processing normally is valid behavior. However, `400` is the stricter/safer choice.

## Sources

- [RFC 9112 Section 5](https://www.rfc-editor.org/rfc/rfc9112#section-5)
