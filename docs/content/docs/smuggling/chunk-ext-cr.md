---
title: "CHUNK-EXT-CR"
description: "CHUNK-EXT-CR test documentation"
weight: 51
---

| | |
|---|---|
| **Test ID** | `SMUG-CHUNK-EXT-CR` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1), [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) |
| **Requirement** | MUST reject malformed chunk line |
| **Expected** | `400` or close |

## What it sends

A chunk-size line where a bare CR appears inside the extension area, not as a valid `CRLF` terminator.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
5;a\rX\r\n
hello\r\n
0\r\n
\r\n
```

## Why it matters

Differential handling of bare CR in framing metadata can produce parser disagreement across hops and create desync risk.

## Sources

- [RFC 9112 §2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2)
- [RFC 9112 §7.1.1](https://www.rfc-editor.org/rfc/rfc9112#section-7.1.1)
