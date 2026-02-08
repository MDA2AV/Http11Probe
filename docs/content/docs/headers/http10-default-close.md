---
title: "HTTP10-DEFAULT-CLOSE"
description: "HTTP10-DEFAULT-CLOSE test documentation"
weight: 7
---

| | |
|---|---|
| **Test ID** | `COMP-HTTP10-DEFAULT-CLOSE` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 §9.3](https://www.rfc-editor.org/rfc/rfc9112#section-9.3) |
| **Requirement** | SHOULD |
| **Expected** | `2xx` + connection closed |

## What it sends

An HTTP/1.0 GET request without a `Connection: keep-alive` header.

```http
GET / HTTP/1.0\r\n
Host: localhost:8080\r\n
\r\n
```

## What the RFC says

HTTP/1.0 connections are not persistent by default. Unlike HTTP/1.1, where persistent connections are the default, an HTTP/1.0 client must explicitly request persistence via `Connection: keep-alive`.

The RFC defines persistence rules by precedence. For HTTP/1.0 without `keep-alive`, the final rule applies:

> "If the 'close' connection option is present (Section 9.6), the connection will not persist after the current response; else, if the received protocol is HTTP/1.1 (or later), the connection will persist after the current response; else, if the received protocol is HTTP/1.0, the 'keep-alive' connection option is present, either the recipient is not a proxy or the message is a response, and the recipient wishes to honor the HTTP/1.0 'keep-alive' mechanism, the connection will persist after the current response; otherwise, the connection will close after the current response." -- RFC 9112 Section 9.3

Without `Connection: keep-alive`, the server should treat the connection as non-persistent and close it after delivering the response.

**Pass:** Server responds `2xx` and closes the connection.
**Warn:** Server responds `2xx` but keeps the connection open (minor violation of SHOULD).

## Why it matters

If a server treats HTTP/1.0 connections as persistent by default, it may hold the connection open indefinitely waiting for another request that will never come, wasting resources. More critically, in proxy chains, a downstream server keeping an HTTP/1.0 connection alive when the proxy expects it to close can cause response desynchronization.

## Deep Analysis

### Relevant ABNF Grammar

```
HTTP-version = HTTP-name "/" DIGIT "." DIGIT
HTTP-name    = %x48.54.54.50 ; "HTTP"
```

An HTTP/1.0 request uses `HTTP/1.0` as the version token. The persistence behavior is not governed by ABNF but by the protocol rules in RFC 9112 Section 9.3.

### RFC Evidence

**RFC 9112 Section 9.3** establishes the default persistence model:

> "HTTP/1.1 defaults to the use of 'persistent connections', allowing multiple requests and responses to be carried over a single connection." -- RFC 9112 Section 9.3

**RFC 9112 Section 9.3** defines the persistence precedence rules, with the final fallback:

> "If the 'close' connection option is present, the connection will not persist after the current response; else, if the received protocol is HTTP/1.1 (or later), the connection will persist after the current response; else, if the received protocol is HTTP/1.0, the 'keep-alive' connection option is present...the connection will persist after the current response; otherwise, the connection will close after the current response." -- RFC 9112 Section 9.3

**RFC 9112 Section 9.3** mandates proxy behavior:

> "A proxy server MUST NOT maintain a persistent connection with an HTTP/1.0 client." -- RFC 9112 Section 9.3

### Chain of Reasoning

1. The test sends an HTTP/1.0 request without `Connection: keep-alive`.
2. Following the persistence precedence in Section 9.3: the `close` option is not present, the protocol is not HTTP/1.1, and `keep-alive` is not present, so the final rule applies -- "the connection will close after the current response."
3. The server should respond normally (2xx) and then close the TCP connection.
4. This is a SHOULD-level behavior because the RFC does not use explicit MUST language for the final fallback rule. The persistence determination is described as a set of conditions rather than a MUST directive.
5. A server that keeps the connection open after an HTTP/1.0 request without keep-alive is wasting resources and may cause proxy desynchronization.

### Scoring Justification

**Scored (SHOULD).** The persistence rules in Section 9.3 describe expected behavior without explicit MUST language for the HTTP/1.0 fallback case. Pass is recorded when the server responds with 2xx and closes the connection. Warn is recorded when the server responds with 2xx but keeps the connection open, as this is a minor violation of the expected default behavior rather than a hard protocol error.

## Sources

- [RFC 9112 Section 9.3 -- Persistence](https://www.rfc-editor.org/rfc/rfc9112#section-9.3)
