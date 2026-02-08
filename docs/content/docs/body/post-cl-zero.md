---
title: "POST-CL-ZERO"
description: "POST-CL-ZERO test documentation"
weight: 2
---

| | |
|---|---|
| **Test ID** | `COMP-POST-CL-ZERO` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2) |
| **Requirement** | MUST accept |
| **Expected** | `2xx` or close |

## What it sends

A POST with `Content-Length: 0` and no body bytes after the headers.

```http
POST / HTTP/1.1\r\n
Host: localhost\r\n
Content-Length: 0\r\n
\r\n
```

## What the RFC says

> "If a valid Content-Length header field is present without Transfer-Encoding, its decimal value defines the expected message body length in octets." — RFC 9112 Section 6.2

A Content-Length of zero is explicitly valid. The server must not block waiting for body bytes that will never arrive.

## Why it matters

Zero-length POSTs are common in APIs (e.g., triggering an action with no payload). A server that hangs waiting for a body on CL:0 will cause client timeouts and connection leaks.

## Sources

- [RFC 9112 Section 6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2)
