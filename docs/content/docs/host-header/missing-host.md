---
title: "MISSING-HOST"
description: "MISSING-HOST test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `RFC9112-7.1-MISSING-HOST` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) |
| **Requirement** | MUST |
| **Expected** | `400` |

## What it sends

A valid `GET / HTTP/1.1` request with no `Host` header.

## What the RFC says

> "A server **MUST** respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field..."

This is one of the strongest requirements in the HTTP spec. The server MUST actually send a 400 response — closing the connection silently does not satisfy this MUST.

## Why it matters

The Host header tells the server which virtual host is being addressed. Without it, the server cannot determine which site the request is for. In multi-tenant environments, processing a request without a Host header could route it to the wrong application.

## Sources

- [RFC 9112 Section 3.2 — Request Target](https://www.rfc-editor.org/rfc/rfc9112#section-3.2)
- [RFC 9110 Section 7.2 — Host and :authority](https://www.rfc-editor.org/rfc/rfc9110#section-7.2)
