---
title: "ACCEPT-NONSENSE"
description: "ACCEPT-NONSENSE test documentation"
weight: 21
---

| | |
|---|---|
| **Test ID** | `COMP-ACCEPT-NONSENSE` |
| **Category** | Compliance |
| **Scored** | No |
| **RFC** | [RFC 9110 §12.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-12.5.1) |
| **RFC Level** | SHOULD |
| **Expected** | `406` preferred, `2xx` acceptable |

## What it sends

A GET request with an `Accept` header requesting a non-existent media type.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Accept: application/x-nonsense\r\n
\r\n
```

## What the RFC says

> "A request without any Accept header field implies that the user agent will accept any media type in response." — RFC 9110 §12.5.1

> "If the header field is present in a request and none of the available representations for the response have a media type that is listed as acceptable, the origin server can either honor the header field by sending a 406 (Not Acceptable) response or disregard the header field by treating the response as if it is not subject to content negotiation for that request." — RFC 9110 §12.5.1

## Why it matters

Content negotiation allows servers to serve different representations of a resource based on client capabilities. A server that returns `406 Not Acceptable` for unrecognized media types actively enforces content negotiation. A server that ignores the `Accept` header and serves a default representation is also compliant — the RFC explicitly allows both behaviors.

## Verdicts

- **Pass** — Server returns `406 Not Acceptable` (enforces content negotiation)
- **Warn** — Server returns `2xx` (ignores Accept, serves default representation)
- **Fail** — Server returns an unexpected error status

## Sources

- [RFC 9110 §12.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-12.5.1)
