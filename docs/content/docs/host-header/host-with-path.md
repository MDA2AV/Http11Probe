---
title: "HOST-WITH-PATH"
description: "HOST-WITH-PATH test documentation"
weight: 5
---

| | |
|---|---|
| **Test ID** | `COMP-HOST-WITH-PATH` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) |
| **Requirement** | MUST respond with 400 |
| **Expected** | `400` or close |

## What it sends

A request with `Host: hostname:port/path`.

## What the RFC says

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that... contains... a Host header field with an invalid field value." — RFC 9112 Section 3.2. The Host header grammar is `uri-host [ ":" port ]` with no path component allowed.

## Why it matters

A path in the Host header is a clear sign of manipulation. If a reverse proxy uses the Host to route, a path component could alter routing.

## Sources

- [RFC 9112 Section 3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2)
