---
title: "GET-CL-BODY-DESYNC"
description: "SMUG-GET-CL-BODY-DESYNC test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `SMUG-GET-CL-BODY-DESYNC` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §9.3.1](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.1) |
| **Requirement** | Unscored |
| **Expected** | `400`/close/pass-through; poisoned follow-up = warn |

## What it sends

A `GET` with `Content-Length: 5` and body `hello`, followed by a second `GET` on the same socket.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 5\r\n
\r\n
hello

GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

> "Content received in a GET request has no generally defined semantics... and might lead some implementations to reject the request and close the connection because of its potential as a request smuggling attack." -- RFC 9110 Section 9.3.1

This test extends the GET-with-body case into a sequence to detect unread-body desynchronization.

## Partial Coverage Note

Existing test `COMP-GET-WITH-CL-BODY` already checks single-request behavior. This test adds a follow-up request to detect connection poisoning.

## Why it matters

Single-request `2xx` is not enough to prove safety. The second request reveals whether body bytes were consumed or leaked into the next parse.

## Sources

- [RFC 9110 §9.3.1](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.1)
