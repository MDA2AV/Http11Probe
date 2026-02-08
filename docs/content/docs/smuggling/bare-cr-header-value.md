---
title: "BARE-CR-HEADER-VALUE"
description: "BARE-CR-HEADER-VALUE test documentation"
weight: 19
---

| | |
|---|---|
| **Test ID** | `SMUG-BARE-CR-HEADER-VALUE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) |
| **Requirement** | MUST reject or replace with SP |
| **Expected** | `400` or close |

## What it sends

Header value containing a bare CR (0x0D not followed by LF).

## What the RFC says

> "A sender MUST NOT generate a bare CR... A recipient of such a bare CR MUST consider that element to be invalid or replace each bare CR with SP..." — RFC 9112 §2.2

## Why it matters

Bare CR in header values can cause different parsers to split headers differently, enabling smuggling.

## Sources

- [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2)
