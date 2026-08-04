---
title: "No CL In 204 — HTTP/1.1 Compliance"
description: "An OPTIONS request to the root path. Some servers respond with 204 No Content, which triggers the validation. Tested against RFC 9110 §8.6."
weight: 3
---

| | |
|---|---|
| **Test ID** | `COMP-NO-CL-IN-204` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 §8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) |
| **Requirement** | MUST NOT |
| **Expected** | `204` without `Content-Length` |

## What it sends

An OPTIONS request to the root path. Some servers respond with `204 No Content`, which triggers the validation.

```http
OPTIONS / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "A server MUST NOT send a Content-Length header field in any response with a status code of 1xx (Informational) or 204 (No Content)." -- RFC 9110 Section 8.6

## Why it matters

A `204 No Content` response explicitly signals that there is no body. Including `Content-Length` contradicts this, and some clients or proxies may attempt to read body bytes based on the Content-Length value. On persistent connections, this causes desync — the client reads the next response's bytes as body data for the 204, corrupting the entire connection. If the server does not return 204 for this request, the test reports a warning since the prohibition cannot be verified.

## Sources

- [RFC 9110 §8.6 -- Content-Length](https://www.rfc-editor.org/rfc/rfc9110#section-8.6)
