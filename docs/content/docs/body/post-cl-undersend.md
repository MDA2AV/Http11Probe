---
title: "POST-CL-UNDERSEND"
description: "POST-CL-UNDERSEND test documentation"
weight: 4
---

| | |
|---|---|
| **Test ID** | `COMP-POST-CL-UNDERSEND` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2) |
| **Requirement** | MUST read declared length |
| **Expected** | `400`, close, or timeout |

## What it sends

A POST declaring `Content-Length: 10` but sending only 5 bytes (`hello`). The connection then goes silent.

```http
POST / HTTP/1.1\r\n
Host: localhost\r\n
Content-Length: 10\r\n
\r\n
hello
```

## What the RFC says

> "If a valid Content-Length header field is present without Transfer-Encoding, its decimal value defines the expected message body length in octets." — RFC 9112 Section 6.2

The server is obligated to read exactly 10 bytes. Since only 5 arrive, the server blocks waiting for the remaining 5 bytes until its read timeout fires.

## Why it matters

A server that responds before reading the full declared body risks desynchronizing the connection — leftover bytes from the incomplete body could be interpreted as the start of the next request, creating a smuggling vector.

## Sources

- [RFC 9112 Section 6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2)
