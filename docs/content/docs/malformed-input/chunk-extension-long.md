---
title: "CHUNK-EXTENSION-LONG"
description: "CHUNK-EXTENSION-LONG test documentation"
weight: 18
---

| | |
|---|---|
| **Test ID** | `MAL-CHUNK-EXTENSION-LONG` |
| **Category** | Malformed Input |
| **Expected** | `400`/`431` or close |

## What it sends

A chunked request with a chunk extension containing 100KB of data.

## Why it matters

While chunk extensions are syntactically valid per RFC 9112 Section 7.1.1, a 100KB extension is pathological. A robust server should reject unreasonably large chunk extensions to prevent resource exhaustion and denial of service.

## Sources

- RFC 9112 Section 7.1.1 — chunk extensions
