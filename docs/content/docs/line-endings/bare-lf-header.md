---
title: "BARE-LF-HEADER"
description: "BARE-LF-HEADER test documentation"
weight: 2
---

| | |
|---|---|
| **Test ID** | `RFC9112-2.2-BARE-LF-HEADER` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) |
| **Requirement** | MAY |
| **Expected** | `400` or close |

## What it sends

A valid `GET` request where one of the header lines is terminated with `\n` (bare LF) instead of `\r\n`.

## What the RFC says

Same rule as for the request-line: the recipient MAY recognize a single LF as a line terminator. Bare LF is a sender violation but recipients are permitted to tolerate it.

## Why it matters

If headers are delimited differently by different parsers in a request chain, an attacker can inject headers that only one parser sees. This is the foundation of header injection and smuggling attacks.

## Sources

- [RFC 9112 Section 2.2 — Message Parsing](https://www.rfc-editor.org/rfc/rfc9112#section-2.2)
