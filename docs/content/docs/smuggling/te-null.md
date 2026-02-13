---
title: "TE-NULL"
description: "TE-NULL test documentation"
weight: 54
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-NULL` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5), [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | MUST reject malformed field value |
| **Expected** | `400` or close |

## What it sends

`Transfer-Encoding: chunked<NUL>` with `Content-Length` present.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\x00\r\n
Content-Length: 5\r\n
\r\n
hello
```

## Why it matters

NUL handling differences (truncate vs reject) are a classic parser differential that can destabilize message framing.

## Sources

- [RFC 9110 §5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5)
- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
