---
title: "LONG-HEADER-NAME"
description: "LONG-HEADER-NAME test documentation"
weight: 3
---

| | |
|---|---|
| **Test ID** | `MAL-LONG-HEADER-NAME` |
| **Category** | Malformed Input |
| **Expected** | `400`, `431`, or close |

## What it sends

A request with a ~100 KB header field name.

## What the RFC says

[RFC 6585 Section 5](https://www.rfc-editor.org/rfc/rfc6585#section-5) defines 431 for this case:

> "The 431 status code indicates that the server is unwilling to process the request because its header fields are too large."

## Sources

- [RFC 6585 Section 5](https://www.rfc-editor.org/rfc/rfc6585#section-5)
