---
title: "EXPECT-100-CL-DESYNC"
description: "SMUG-EXPECT-100-CL-DESYNC test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `SMUG-EXPECT-100-CL-DESYNC` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §10.1.1](https://www.rfc-editor.org/rfc/rfc9110#section-10.1.1) |
| **Requirement** | Unscored |
| **Expected** | `417/400/close` preferred; poisoned follow-up = warn |

## What it sends

A `POST` with `Expect: 100-continue` and immediate body bytes, followed by a second `GET` on the same connection.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 5\r\n
Expect: 100-continue\r\n
\r\n
hello

GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "The 100 (Continue) interim response indicates that the initial part of a request has been received and has not yet been rejected by the server." -- RFC 9110 Section 10.1.1

This test checks whether servers that accept this flow keep connection framing safe for the next request.

## Partial Coverage Note

Existing test `SMUG-EXPECT-100-CL` checks one request. This desync variant verifies the post-response connection state using a second request.

## Why it matters

Desync risk appears when a server issues a final response without fully consuming the declared body.

## Sources

- [RFC 9110 §10.1.1](https://www.rfc-editor.org/rfc/rfc9110#section-10.1.1)
