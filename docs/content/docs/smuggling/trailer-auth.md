---
title: "TRAILER-AUTH"
description: "TRAILER-AUTH test documentation"
weight: 55
---

| | |
|---|---|
| **Test ID** | `SMUG-TRAILER-AUTH` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §6.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-6.5.1) |
| **Requirement** | Unscored |
| **Expected** | `400` or `2xx` |

## What it sends

A chunked request that places `Authorization` in the trailer section.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
\r\n
5\r\n
hello\r\n
0\r\n
Authorization: Bearer evil\r\n
\r\n
```

## Why this test is unscored

`Authorization` in trailers is prohibited for senders, but recipients can either reject or ignore/discard it. Status code alone cannot prove whether downstream components consumed it.

## Sources

- [RFC 9110 §6.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-6.5.1)
