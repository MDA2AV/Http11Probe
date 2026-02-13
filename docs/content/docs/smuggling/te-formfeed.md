---
title: "TE-FORMFEED"
description: "TE-FORMFEED test documentation"
weight: 53
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-FORMFEED` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5), [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | MUST reject invalid transfer-coding token |
| **Expected** | `400` or close |

## What it sends

`Transfer-Encoding: <FF>chunked` with `Content-Length` present.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: \x0cchunked\r\n
Content-Length: 5\r\n
\r\n
hello
```

## Why it matters

Form-feed control characters in TE values are an obfuscation vector that can trigger parser disagreement in proxy chains.

## Sources

- [RFC 9110 §5.5](https://www.rfc-editor.org/rfc/rfc9110#section-5.5)
- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
