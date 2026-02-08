---
title: "POST-CL-BODY"
description: "POST-CL-BODY test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `COMP-POST-CL-BODY` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2) |
| **Requirement** | MUST accept |
| **Expected** | `2xx` |

## What it sends

A valid POST with `Content-Length: 5` and exactly 5 bytes of body (`hello`).

```http
POST / HTTP/1.1\r\n
Host: localhost\r\n
Content-Length: 5\r\n
\r\n
hello
```

## What the RFC says

> "If a valid Content-Length header field is present without Transfer-Encoding, its decimal value defines the expected message body length in octets." — RFC 9112 Section 6.2

The server must read exactly 5 bytes from the connection after the header section ends, then process the request normally.

## Why it matters

This is the most basic body consumption test. If a server cannot read a fixed-length POST body, it cannot handle form submissions, API calls, or file uploads — the foundation of any interactive web application.

## Sources

- [RFC 9112 Section 6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2)
