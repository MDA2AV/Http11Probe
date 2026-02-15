---
title: "OPTIONS-CL-BODY-DESYNC"
description: "SMUG-OPTIONS-CL-BODY-DESYNC test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `SMUG-OPTIONS-CL-BODY-DESYNC` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §9.3.7](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.7) |
| **Requirement** | Unscored |
| **Expected** | `400`/close/pass-through; poisoned follow-up = warn |

## What it sends

An `OPTIONS` request with `Content-Length: 5` and body `hello`, followed by a second `GET` on the same connection.

```http
OPTIONS / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 5\r\n
\r\n
hello

GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "An OPTIONS request containing content must send a valid Content-Type header field describing the representation media type." -- RFC 9110 Section 9.3.7

While server behavior differs across frameworks, this sequence checks whether body handling leaves the connection in a desynchronized state.

## Partial Coverage Note

Existing test `SMUG-OPTIONS-CL-BODY` checks a single request. This variant adds a follow-up request to detect unread-body poisoning.

## Why it matters

OPTIONS is common in CORS workflows. If a server responds before consuming bytes, those bytes can corrupt the next request boundary.

## Sources

- [RFC 9110 §9.3.7](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.7)
