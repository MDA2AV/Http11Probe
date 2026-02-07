---
title: "HTTP09-REQUEST"
weight: 5
---

| | |
|---|---|
| **Test ID** | `RFC9112-2.3-HTTP09-REQUEST` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 2.3](https://www.rfc-editor.org/rfc/rfc9112#section-2.3) |
| **Requirement** | SHOULD |
| **Expected** | `400`, close, or timeout |

## What it sends

An HTTP/0.9 style request: `GET /\r\n` — a method and target with no HTTP version.

## What the RFC says

Without a version, the request-line does not match the `method SP request-target SP HTTP-version CRLF` grammar. It is an invalid request-line, and the SHOULD from Section 3 applies. The server may also not recognize this as a request at all, hence timeout is acceptable.

## Why it matters

HTTP/0.9 was a protocol from 1991 with no headers, no status codes, and no Content-Length. It has no place in modern infrastructure. A server that attempts to process HTTP/0.9 requests is exposing legacy attack surface.

## Sources

- [RFC 9112 Section 2.3 — HTTP Version](https://www.rfc-editor.org/rfc/rfc9112#section-2.3)
- [RFC 9112 Section 3 — Request Line](https://www.rfc-editor.org/rfc/rfc9112#section-3)
