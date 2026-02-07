---
title: "WHITESPACE-ONLY-LINE"
description: "WHITESPACE-ONLY-LINE test documentation"
weight: 14
---

| | |
|---|---|
| **Test ID** | `MAL-WHITESPACE-ONLY-LINE` |
| **Category** | Malformed Input |
| **Expected** | `400`, close, or timeout |

## What it sends

A line consisting only of spaces and tabs -- no method, URI, or version.

## Why it matters

RFC 9112 Section 2.2 allows servers to ignore an empty line before the request-line. But a line of only whitespace is neither empty nor a valid request-line.

## Sources

- [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2)
