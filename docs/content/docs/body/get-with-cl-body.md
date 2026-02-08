---
title: "GET-WITH-CL-BODY"
description: "GET-WITH-CL-BODY test documentation"
weight: 5
---

| | |
|---|---|
| **Test ID** | `COMP-GET-WITH-CL-BODY` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 Section 9.3.1](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.1) |
| **Requirement** | MAY reject |
| **Expected** | `400` = Pass; `2xx` = Warn |

## What it sends

A GET request with `Content-Length: 5` and a body (`hello`).

```http
GET / HTTP/1.1\r\n
Host: localhost\r\n
Content-Length: 5\r\n
\r\n
hello
```

## What the RFC says

> "Although request message framing is independent of method semantics, content received in a GET request has no generally defined semantics, cannot alter the meaning or target of the request, and might lead some implementations to reject the request and close the connection because of its potential as a request smuggling attack." — RFC 9110 Section 9.3.1

A body on GET is unusual and has no defined semantics. Rejecting it is stricter and safer.

## Why it matters

GET-with-body is a known smuggling vector. If a front-end proxy strips the body but a back-end server reads it, the leftover bytes desynchronize the connection. Rejecting GET bodies at the server level eliminates this attack surface.

## Sources

- [RFC 9110 Section 9.3.1](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.1)
