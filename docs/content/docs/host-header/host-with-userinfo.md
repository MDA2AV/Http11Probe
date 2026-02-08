---
title: "HOST-WITH-USERINFO"
description: "HOST-WITH-USERINFO test documentation"
weight: 4
---

| | |
|---|---|
| **Test ID** | `COMP-HOST-WITH-USERINFO` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) |
| **Requirement** | MUST respond with 400 |
| **Expected** | `400` or close |

## What it sends

A request with `Host: user@hostname:port`.

## What the RFC says

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that... contains... a Host header field with an invalid field value." — RFC 9112 Section 3.2. The Host header grammar is `uri-host [ ":" port ]` — no userinfo is permitted.

## Why it matters

The userinfo component (`user@`) is not part of the Host grammar. A server that accepts it may be tricked into routing requests incorrectly.

## Sources

- [RFC 9112 Section 3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2)
- [RFC 3986 Section 3.2.1](https://www.rfc-editor.org/rfc/rfc3986#section-3.2.1)
