---
title: "METHOD-CASE"
description: "METHOD-CASE test documentation"
weight: 10
---

| | |
|---|---|
| **Test ID** | `COMP-METHOD-CASE` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 Section 9.1](https://www.rfc-editor.org/rfc/rfc9110#section-9.1) |
| **Requirement** | Case-sensitive (unscored) |
| **Expected** | `400`/`405`/`501` or `2xx` |

## What it sends

`get / HTTP/1.1` — lowercase method name.

## What the RFC says

> "The method token is case-sensitive because it might be used as a gateway to object-based systems with case-sensitive method names." — RFC 9110 Section 9.1

## Why it matters

This is an unscored test. The method token is case-sensitive by definition. A server that accepts `get` treats methods case-insensitively, which works in practice but deviates from the spec.

## Sources

- [RFC 9110 Section 9.1](https://www.rfc-editor.org/rfc/rfc9110#section-9.1)
