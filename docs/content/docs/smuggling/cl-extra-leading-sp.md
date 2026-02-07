---
title: "CL-EXTRA-LEADING-SP"
weight: 11
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-EXTRA-LEADING-SP` |
| **Category** | Smuggling (Unscored) |
| **Expected** | `400` (strict) or `2xx` (RFC-compliant) |

## What it sends

`Content-Length:  0` — extra space between colon and value.

## Why it's unscored

OWS before the field value is permitted. The server may trim it and process normally.

## Sources

- [RFC 9112 Section 5](https://www.rfc-editor.org/rfc/rfc9112#section-5)
