---
title: "MISSING-HOST"
description: "MISSING-HOST test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `RFC9112-7.1-MISSING-HOST` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 3.2](https://www.rfc-editor.org/rfc/rfc9112#section-3.2) |
| **Requirement** | MUST |
| **Expected** | `400` |

## What it sends

A valid `GET / HTTP/1.1` request with no `Host` header.

```http
GET / HTTP/1.1\r\n
\r\n
```

No `Host` header at all.


## What the RFC says

> "A client MUST send a Host header field in all HTTP/1.1 request messages." -- RFC 9112 Section 3.2

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field and to any request message that contains more than one Host header field line or a Host header field with an invalid field value." -- RFC 9112 Section 3.2

This is one of the strongest requirements in the HTTP spec. The server MUST actually send a 400 response -- closing the connection silently does not satisfy this MUST.

## Why it matters

The Host header tells the server which virtual host is being addressed. Without it, the server cannot determine which site the request is for. In multi-tenant environments, processing a request without a Host header could route it to the wrong application.

## Deep Analysis

### Relevant ABNF Grammar

```
Host = uri-host [ ":" port ]
```

The Host header carries the authority information for the target URI. In an origin-form request (`GET / HTTP/1.1`), the Host header is the sole source of authority, making it indispensable for request routing.

### RFC Evidence

**RFC 9112 Section 3.2** mandates the client obligation:

> "A client MUST send a Host header field in all HTTP/1.1 request messages." -- RFC 9112 Section 3.2

**RFC 9112 Section 3.2** mandates the server response with a specific status code:

> "A server MUST respond with a 400 (Bad Request) status code to any HTTP/1.1 request message that lacks a Host header field and to any request message that contains more than one Host header field line or a Host header field with an invalid field value." -- RFC 9112 Section 3.2

**RFC 9110 Section 7.2** reinforces the requirement:

> "A client MUST send the Host header field in an HTTP/1.1 request message, unless the request target is a URI whose origin is undefined." -- RFC 9110 Section 7.2

### Chain of Reasoning

1. The test sends `GET / HTTP/1.1` with no Host header at all.
2. The request uses origin-form (`/`), which does not embed authority information in the request-target. The Host header is the only mechanism to convey which server or virtual host is being addressed.
3. RFC 9112 Section 3.2 uses "MUST respond with a 400" -- not "MUST reject" or "SHOULD respond." The status code 400 is explicitly mandated.
4. Connection close alone does not satisfy this requirement because the RFC specifies the exact response status code the server must use. A server that closes the TCP connection without sending a 400 response is non-compliant.
5. This is one of the few places in the HTTP specification where a specific status code is mandated at the MUST level, reflecting the critical role of the Host header in virtual hosting and request routing.

### Scoring Justification

**Scored (MUST).** The RFC mandates exactly 400 (Bad Request) for a missing Host header in HTTP/1.1. No alternative disposition is offered. Connection close without a 400 response is non-compliant. The `AllowConnectionClose` flag is not set because the RFC explicitly requires the 400 status code to be sent.

## Sources

- [RFC 9112 Section 3.2 -- Request Target](https://www.rfc-editor.org/rfc/rfc9112#section-3.2)
- [RFC 9110 Section 7.2 -- Host and :authority](https://www.rfc-editor.org/rfc/rfc9110#section-7.2)
