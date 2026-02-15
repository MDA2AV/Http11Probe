---
title: "POST-UNSUPPORTED-CT"
description: "POST-UNSUPPORTED-CT test documentation"
weight: 15
---

| | |
|---|---|
| **Test ID** | `COMP-POST-UNSUPPORTED-CT` |
| **Category** | Compliance |
| **Scored** | No |
| **RFC** | [RFC 9110 §15.5.16](https://www.rfc-editor.org/rfc/rfc9110#section-15.5.16) |
| **RFC Level** | MAY |
| **Expected** | `415` or `2xx` |

## What it sends

A POST request with an unrecognized `Content-Type`.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 5\r\n
Content-Type: application/x-nonsense\r\n
\r\n
hello
```

## What the RFC says

> "The 415 (Unsupported Media Type) status code indicates that the origin server is refusing to service the request because the content is in a format not supported by this method on the target resource." — RFC 9110 §15.5.16

The server is not required to reject unsupported content types — it may choose to accept the body regardless of the declared type.

## Why it matters

A server that validates `Content-Type` and returns `415` for unsupported formats provides better API hygiene, helping clients detect misconfigured requests early. A server that ignores unknown content types and processes the body anyway is also valid behavior — many servers treat the body as opaque bytes regardless of the declared type.

## Verdicts

- **Pass** — Server returns `415` (validates content type) or `2xx` (accepts any type)
- **Warn** — Server returns an unexpected status

## Sources

- [RFC 9110 §15.5.16](https://www.rfc-editor.org/rfc/rfc9110#section-15.5.16)
