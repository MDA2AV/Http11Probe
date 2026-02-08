---
title: "TE-HTTP10"
description: "TE-HTTP10 test documentation"
weight: 17
---

| | |
|---|---|
| **Test ID** | `SMUG-TE-HTTP10` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | MUST reject |
| **Expected** | `400` or close |

## What it sends

HTTP/1.0 request with `Transfer-Encoding: chunked` and `Content-Length: 5`.

```http
POST / HTTP/1.0\r\n
Host: localhost:8080\r\n
Transfer-Encoding: chunked\r\n
Content-Length: 5\r\n
\r\n
hello
```

Note the HTTP/1.0 version — Transfer-Encoding is not defined for HTTP/1.0.


## What the RFC says

> "A server MUST NOT send a response containing Transfer-Encoding unless the corresponding request indicates HTTP/1.1 (or later minor revisions)." — RFC 9112 Section 6.1

> "A server or client that receives an HTTP/1.0 message containing a Transfer-Encoding header field MUST treat the message as if the framing is faulty, even if a Content-Length is present, and close the connection." — RFC 9112 Section 6.1

Transfer-Encoding is not defined in HTTP/1.0. The RFC explicitly requires treating TE in a 1.0 message as faulty framing.

## Why it matters

HTTP/1.0 doesn't support chunked encoding. A server that processes TE on a 1.0 request may disagree with proxies that use CL.

## Deep Analysis

### ABNF

```
Transfer-Encoding = #transfer-coding       ; RFC 9112 §6.1
transfer-coding   = token                  ; RFC 9110 §10.1.4
HTTP-version      = HTTP-name "/" DIGIT "." DIGIT  ; RFC 9112 §2.6
```

The Transfer-Encoding mechanism is defined exclusively for HTTP/1.1 and later. HTTP/1.0 has no concept of transfer codings; its only body-length mechanism is Content-Length or connection close.

### RFC Evidence

> "A server MUST NOT send a response containing Transfer-Encoding unless the corresponding request indicates HTTP/1.1 (or later minor revisions)." -- RFC 9112 §6.1

> "A server or client that receives an HTTP/1.0 message containing a Transfer-Encoding header field MUST treat the message as if the framing is faulty, even if a Content-Length is present, and close the connection after processing the message." -- RFC 9112 §6.1

> "Transfer-Encoding was added in HTTP/1.1. It is generally assumed that implementations advertising only HTTP/1.0 support will not understand how to process transfer-encoded content, and that an HTTP/1.0 message received with a Transfer-Encoding is likely to have been forwarded without proper handling of the chunked transfer coding in transit." -- RFC 9112 §6.1

### Chain of Reasoning

1. The test sends an `HTTP/1.0` request with `Transfer-Encoding: chunked` and `Content-Length: 5`.
2. RFC 9112 section 6.1 uses **MUST-level** language: a recipient of an HTTP/1.0 message containing Transfer-Encoding "MUST treat the message as if the framing is faulty, even if a Content-Length is present, and close the connection."
3. This is one of the strongest requirements in the specification. The word "faulty" combined with MUST means the server has no discretion -- it cannot process the Transfer-Encoding, it cannot fall back to Content-Length, it must treat the framing as broken.
4. The RFC further explains the rationale: an HTTP/1.0 message with Transfer-Encoding was likely forwarded by an intermediary that did not properly handle chunked encoding in transit. The message may have been corrupted.
5. The combination of `HTTP/1.0` + `Transfer-Encoding` + `Content-Length` creates a scenario where every possible framing interpretation is unreliable.

### Scored / Unscored Justification

This test is **scored** (MUST reject). RFC 9112 section 6.1 contains explicit MUST-level language requiring the server to treat an HTTP/1.0 message with Transfer-Encoding as having faulty framing. There is no room for lenient interpretation -- the server must close the connection.

- **Pass (400 or close):** The server correctly treats the framing as faulty and closes the connection.
- **Fail (2xx):** The server processed Transfer-Encoding on an HTTP/1.0 request, violating a MUST-level requirement.

### Smuggling Attack Scenarios

- **Version Downgrade Desync:** An attacker sends an HTTP/1.0 request with both `Transfer-Encoding: chunked` and `Content-Length: 5`. A front-end proxy that supports HTTP/1.1 may upgrade the request or process the Transfer-Encoding header despite the 1.0 version. A back-end that strictly follows RFC 9112 rejects the request. But if the front-end already forwarded the body using chunked framing to the back-end's connection, leftover bytes on the socket become the next "request" -- a classic smuggling scenario.
- **Proxy Chain Confusion:** In a multi-hop proxy chain, an HTTP/1.0 request with Transfer-Encoding may be handled differently at each hop. A first proxy may strip Transfer-Encoding (treating it as invalid for 1.0), while a second proxy may have already processed the body as chunked. The inconsistency between what each hop consumed leaves attacker-controlled data on the wire.
- **Legacy Server Exploitation:** Older HTTP/1.0-only servers have no concept of chunked encoding. If they receive `Transfer-Encoding: chunked`, they ignore the header entirely and use Content-Length. An intermediary that processes the chunked encoding before forwarding may inject additional data that the legacy server interprets as a separate request.

## Sources

- [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
