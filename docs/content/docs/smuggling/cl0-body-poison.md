---
title: "CL0-BODY-POISON"
description: "SMUG-CL0-BODY-POISON test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `SMUG-CL0-BODY-POISON` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2) |
| **Requirement** | Unscored |
| **Expected** | `400`/close preferred; poisoned follow-up = warn |

## What it sends

A two-step sequence: first a `POST` with `Content-Length: 0` plus one extra byte, then a clean `GET` on the same connection.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 0\r\n
\r\n
X

GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "If a valid Content-Length header field is present without Transfer-Encoding, its decimal value defines the expected message body length in octets." -- RFC 9112 Section 6.2

`Content-Length: 0` means no body bytes are part of the first request. This test checks whether trailing bytes can poison parsing of the next request on a keep-alive connection.

## Why it matters

`0.CL`-style desync chains rely on parser disagreement about where the first request ends. This sequence test surfaces that behavior directly.

## Sources

- [RFC 9112 §6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2)
