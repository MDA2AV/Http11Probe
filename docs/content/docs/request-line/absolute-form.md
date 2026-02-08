---
title: "ABSOLUTE-FORM"
description: "ABSOLUTE-FORM test documentation"
weight: 9
---

| | |
|---|---|
| **Test ID** | `COMP-ABSOLUTE-FORM` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.2) |
| **Requirement** | MUST accept when acting as proxy (unscored) |
| **Expected** | `400` or `2xx` |

## What it sends

`GET http://host/ HTTP/1.1` — the absolute-form request-target.

## What the RFC says

> "When making a request to a proxy, other than a CONNECT or server-wide OPTIONS request, a client MUST send the target URI in absolute-form as the request-target." — RFC 9112 Section 3.2.2. A server MUST accept absolute-form requests.

## Why it matters

This is an unscored test. Non-proxy servers commonly reject absolute-form. Both `400` and `2xx` are acceptable.

## Sources

- [RFC 9112 Section 3.2.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.2)
