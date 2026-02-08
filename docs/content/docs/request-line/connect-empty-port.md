---
title: "CONNECT-EMPTY-PORT"
description: "CONNECT-EMPTY-PORT test documentation"
weight: 11
---

| | |
|---|---|
| **Test ID** | `COMP-CONNECT-EMPTY-PORT` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2.3](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.3) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

`CONNECT host: HTTP/1.1` — authority-form with empty port.

## What the RFC says

> "The authority-form of request-target is only used for CONNECT requests... authority = uri-host ":" port" — RFC 9112 Section 3.2.3. An empty port is not a valid port.

## Why it matters

CONNECT with an empty port is syntactically invalid. Accepting it could cause undefined proxy behavior.

## Sources

- [RFC 9112 Section 3.2.3](https://www.rfc-editor.org/rfc/rfc9112#section-3.2.3)
