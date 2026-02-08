---
title: "CL-INTERNAL-SPACE"
description: "CL-INTERNAL-SPACE test documentation"
weight: 27
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-INTERNAL-SPACE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

`Content-Length: 1 0` — space inside the number.

## What the RFC says

> CL grammar is `1*DIGIT` with no internal whitespace.

## Why it matters

A server that interprets `1 0` as 10 reads more data than one that reads only 1 byte or rejects.

## Sources

- [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
