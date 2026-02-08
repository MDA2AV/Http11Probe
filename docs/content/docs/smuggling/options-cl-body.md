---
title: "OPTIONS-CL-BODY"
description: "OPTIONS-CL-BODY test documentation"
weight: 36
---

| | |
|---|---|
| **Test ID** | `SMUG-OPTIONS-CL-BODY` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §9.3.7](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.7) |
| **Requirement** | Unscored |
| **Expected** | `400` or `2xx` |

## What it sends

`OPTIONS / HTTP/1.1` with `Content-Length: 5` and body `hello`.

```http
OPTIONS / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 5\r\n
\r\n
hello
```


## What the RFC says

> "A client that generates an OPTIONS request containing content MUST send a valid Content-Type header field describing the representation media type. Note that this specification does not define any use for such content." — RFC 9110 §9.3.7

OPTIONS requests may have a body, but the server must properly handle it. This test sends a body without a Content-Type header, which violates the client-side MUST. If the body is not consumed, it leaks onto the connection.

## Why this test is unscored

The RFC explicitly allows bodies on OPTIONS requests (with proper Content-Type). Whether the server rejects the request (`400`) or accepts it and processes the body (`2xx`) are both valid behaviors. The critical requirement is that the server must not leave unread body bytes on the connection.

**Pass:** Server rejects with `400` (strict, safe).
**Warn:** Server accepts and responds `2xx` (must consume body bytes).

## Why it matters

Like HEAD with a body, if the server responds to OPTIONS without reading the declared body bytes, the remaining data is misinterpreted as the next request. This desync can be exploited to smuggle requests, especially when OPTIONS is commonly used for CORS preflight.

## Deep Analysis

### RFC Evidence

> "A client that generates an OPTIONS request containing content MUST send a valid Content-Type header field describing the representation media type. Note that this specification does not define any use for such content." -- RFC 9110 Section 9.3.7

> "The HEAD method is identical to GET except that the server MUST NOT send content in the response." -- RFC 9110 Section 9.3.2

The RFC explicitly permits bodies in OPTIONS requests (unlike HEAD responses, which are forbidden). However, the client-side MUST for Content-Type is important: this test omits Content-Type, meaning the sender is already in violation.

> "Regardless of the method, if a server receives a request with a message body and a Content-Length field, it MUST either read and discard the body bytes or close the connection." -- RFC 9112 Section 6.3 (paraphrased from body length determination rules)

### Chain of Reasoning

1. **OPTIONS with a body is explicitly allowed by the RFC.** Section 9.3.7 does not use MUST NOT or SHOULD NOT for the body itself -- it only requires that a client sending content MUST include Content-Type. This test deliberately omits Content-Type to probe whether the server enforces that client-side requirement. Regardless, the body's 5 bytes are declared via `Content-Length: 5`.

2. **The server must consume or reject the declared body.** The HTTP/1.1 message body length algorithm in RFC 9112 Section 6.3 is clear: when Content-Length is present, it defines the body length. The server must read exactly that many bytes from the connection before attempting to parse the next request. This applies to all methods, including OPTIONS.

3. **OPTIONS responses are typically small and fast.** Most servers respond to `OPTIONS /` immediately with `Allow` headers and a `200 OK` with zero-length body. This fast-path processing makes it easy for an implementation to skip body consumption -- the server "knows" the answer without reading the body. But skipping the body read leaves 5 bytes on the connection.

4. **CORS preflight makes OPTIONS ubiquitous.** In modern web applications, browsers send OPTIONS requests for CORS preflight. This means OPTIONS requests are extremely common in proxy-origin chains. If a server mishandles the body on OPTIONS, the desync is exploitable at scale -- every CORS-enabled endpoint becomes a potential smuggling vector.

5. **Attack scenario.** An attacker sends `OPTIONS / HTTP/1.1` with `Content-Length: N` where the body contains a crafted HTTP request (e.g., `POST /transfer HTTP/1.1\r\nHost: bank.com\r\n...`). The server responds with `200 OK` and the `Allow` header, without consuming the body. The smuggled POST request sits on the connection and is parsed as the next request. On a shared connection behind a reverse proxy, this smuggled request may execute with the next legitimate user's session cookies.

### Scored / Unscored Justification

This test is **unscored** (`Scored = false`). The RFC explicitly allows bodies in OPTIONS requests, and the server has discretion in how it handles one: reject it (`400`, perhaps because Content-Type is missing), accept it and process normally (`2xx`), or even ignore the body content while still properly draining the declared bytes. All of these are defensible behaviors. The dangerous case -- where the server responds without consuming the body -- cannot be definitively detected from the status code alone. A `2xx` response might mean the server properly drained the body (safe) or skipped it entirely (vulnerable). The test flags `2xx` as a warning to prompt investigation of the server's connection behavior.

## Sources

- [RFC 9110 §9.3.7](https://www.rfc-editor.org/rfc/rfc9110#section-9.3.7)
