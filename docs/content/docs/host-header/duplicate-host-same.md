---
title: "DUPLICATE-HOST-SAME"
description: "DUPLICATE-HOST-SAME test documentation"
weight: 3
---

| | |
|---|---|
| **Test ID** | `COMP-DUPLICATE-HOST-SAME` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) |
| **Requirement** | MUST respond with 400 |
| **Expected** | `400` |

## What it sends

A request with two identical Host headers.

## What the RFC says

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that... contains more than one Host header field line..." — RFC 9112 Section 3.2

## Why it matters

The RFC mandates 400 for *any* duplicate Host headers, regardless of whether the values match. Some servers incorrectly allow identical duplicates.

## Sources

- [RFC 9112 Section 3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2)
