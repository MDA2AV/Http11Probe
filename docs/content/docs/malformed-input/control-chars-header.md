---
title: "CONTROL-CHARS-HEADER"
weight: 8
---

| | |
|---|---|
| **Test ID** | `MAL-CONTROL-CHARS-HEADER` |
| **Category** | Malformed Input |
| **RFC** | [RFC 9110 Section 5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5) |
| **Expected** | `400` or close |

## What it sends

A request with control characters (`\x01`-`\x08`, `\x0E`-`\x1F`) in a header field value.

## What the RFC says

RFC 9110 Section 5.5 defines allowed characters in field values. Control characters other than HTAB are not included.

## Sources

- [RFC 9110 Section 5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5)
