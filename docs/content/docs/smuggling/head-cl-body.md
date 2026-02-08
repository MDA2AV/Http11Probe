---
title: "HEAD-CL-BODY"
description: "HEAD-CL-BODY test documentation"
weight: 35
---

| | |
|---|---|
| **Test ID** | `SMUG-HEAD-CL-BODY` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §9.3.2](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.2) |
| **Requirement** | Unscored |
| **Expected** | `400` or `2xx` |

## What it sends

`HEAD / HTTP/1.1` with `Content-Length: 5` and body `hello`. HEAD requests are not supposed to have a response body, but this test sends a request body.

```http
HEAD / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 5\r\n
\r\n
hello
```


## What the RFC says

> "The HEAD method is identical to GET except that the server MUST NOT send content in the response." — RFC 9110 §9.3.2

> "content received in a HEAD request has no generally defined semantics, cannot alter the meaning or target of the request, and might lead some implementations to reject the request and close the connection because of its potential as a request smuggling attack" — RFC 9110 §9.3.2

> "A client SHOULD NOT generate content in a HEAD request unless it is made directly to an origin server that has previously indicated, in or out of band, that such a request has a purpose and will be adequately supported." — RFC 9110 §9.3.2

The RFC does not prohibit a request body on HEAD, but the server must properly consume or discard any sent body to prevent it from spilling into the next request on the connection.

## Why this test is unscored

The RFC does not prohibit sending a body with HEAD requests. Whether the server rejects the request (`400`) or accepts it and properly consumes the body (`2xx`) are both valid behaviors. The critical requirement is that the server must not leave unread body bytes on the connection.

**Pass:** Server rejects with `400` (strict, safe).
**Warn:** Server accepts and responds `2xx` (must consume body bytes).

## Why it matters

If a server responds to HEAD without reading the `Content-Length` worth of body bytes, those bytes remain on the connection and are interpreted as the start of the next request. This is a connection desync that an attacker can exploit for smuggling on persistent connections.

## Deep Analysis

### RFC Evidence

> "The HEAD method is identical to GET except that the server MUST NOT send content in the response." -- RFC 9110 Section 9.3.2

> "content received in a HEAD request has no generally defined semantics, cannot alter the meaning or target of the request, and might lead some implementations to reject the request and close the connection because of its potential as a request smuggling attack" -- RFC 9110 Section 9.3.2

> "A client SHOULD NOT generate content in a HEAD request unless it is made directly to an origin server that has previously indicated, in or out of band, that such a request has a purpose and will be adequately supported." -- RFC 9110 Section 9.3.2

### Chain of Reasoning

1. **HEAD with a body is unusual but not prohibited.** The RFC uses SHOULD NOT (not MUST NOT) for the client side, meaning sending content with HEAD is discouraged but technically allowed. The server side has no explicit MUST about rejecting body content on HEAD requests. This gray area is exactly what makes the test interesting from a smuggling perspective.

2. **The server's response framing is the key issue.** HEAD responses MUST NOT include a response body, but they MAY include `Content-Length` indicating what the equivalent GET response would return. This means the server produces a response quickly -- potentially before it reads the 5 bytes of request body declared by `Content-Length: 5`.

3. **Unconsumed body bytes become a smuggled request.** If the server responds to the HEAD without draining the request body, the 5 bytes (`hello`) remain on the TCP connection. On a keep-alive connection, the server's HTTP parser will attempt to read these bytes as the start-line of the next request. The parser sees `hello` where it expects a method like `GET` or `POST`, which may cause an error -- or, with carefully crafted content, it could be parsed as a valid request.

4. **Attack scenario.** An attacker sends `HEAD / HTTP/1.1` with `Content-Length: N` where the body contains a complete smuggled HTTP request (e.g., `GET /admin HTTP/1.1\r\nHost: target\r\n\r\n`). The server processes the HEAD, sends back headers without a body, and if it does not consume the `N` bytes of declared content, the smuggled `GET /admin` request is parsed as the next request on the connection. On a shared connection (e.g., behind a load balancer), this smuggled request executes with the next legitimate user's credentials.

### Scored / Unscored Justification

This test is **unscored** (`Scored = false`). The RFC explicitly acknowledges that HEAD requests with content have "no generally defined semantics" and that implementations may reject them. Neither rejection (`400`) nor acceptance (`2xx`) violates a MUST-level requirement. The real danger -- whether the server leaves body bytes on the connection -- is a behavioral property that cannot be reliably determined from the status code alone. A `2xx` response could mean the server properly consumed the body (safe) or ignored it (vulnerable). The test flags `2xx` as a warning to prompt manual investigation of the server's connection handling.

## Sources

- [RFC 9110 §9.3.2](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.2)
