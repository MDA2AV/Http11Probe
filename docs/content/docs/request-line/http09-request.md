---
title: "HTTP09-REQUEST"
description: "HTTP09-REQUEST test documentation"
weight: 5
---

| | |
|---|---|
| **Test ID** | `RFC9112-2.3-HTTP09-REQUEST` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 2.3](https://www.rfc-editor.org/rfc/rfc9112#section-2.3) |
| **Requirement** | SHOULD |
| **Expected** | `400`, close, or timeout |

## What it sends

An HTTP/0.9 style request: `GET /\r\n` — a method and target with no HTTP version.

```http
GET /\r\n
```

No HTTP version, no headers, no blank line — just a raw path terminated by CRLF.


## What the RFC says

The request-line grammar requires a version string:

> "request-line = method SP request-target SP HTTP-version" -- RFC 9112 §3

Without a version, the request-line does not match this grammar. It is an invalid request-line:

> "Recipients of an invalid request-line SHOULD respond with either a 400 (Bad Request) error or a 301 (Moved Permanently) redirect with the request-target properly encoded." — RFC 9112 Section 3

The server may also not recognize this as a request at all, hence timeout is acceptable.

## Why it matters

HTTP/0.9 was a protocol from 1991 with no headers, no status codes, and no Content-Length. It has no place in modern infrastructure. A server that attempts to process HTTP/0.9 requests is exposing legacy attack surface.

## Deep Analysis

### Relevant ABNF Grammar

```
request-line = method SP request-target SP HTTP-version CRLF
HTTP-version = HTTP-name "/" DIGIT "." DIGIT
HTTP-name    = %s"HTTP"
```

The `request-line` grammar requires three components separated by spaces: `method`, `request-target`, and `HTTP-version`, terminated by CRLF. An HTTP/0.9 request (`GET /\r\n`) contains only two tokens (method and target) with no version string, and is immediately terminated by CRLF.

### RFC Evidence

**RFC 9112 Section 3** defines the required request-line structure:

> "request-line = method SP request-target SP HTTP-version" -- RFC 9112 Section 3

**RFC 9112 Section 2.3** defines the HTTP-version format and its role:

> "HTTP uses a '<major>.<minor>' numbering scheme to indicate versions of the protocol. This specification defines version '1.1'." -- RFC 9112 Section 2.3

**RFC 9112 Section 3** provides guidance for invalid request-lines:

> "Recipients of an invalid request-line SHOULD respond with either a 400 (Bad Request) error or a 301 (Moved Permanently) redirect with the request-target properly encoded." -- RFC 9112 Section 3

### Chain of Reasoning

1. The request-line ABNF requires exactly three space-delimited tokens: method, request-target, and HTTP-version. `GET /\r\n` has only two tokens.
2. Without an HTTP-version token, the message does not match the `request-line` production at all. It is structurally incomplete.
3. HTTP/0.9 was a 1991-era protocol that predated headers, status codes, and content framing. RFC 9112 does not define support for HTTP/0.9 -- the version field is mandatory.
4. A server encountering `GET /\r\n` without further data faces an ambiguity: it could be an incomplete HTTP/1.x request (more data coming) or a complete HTTP/0.9 request. Waiting for more data (timeout) is a legitimate interpretation.
5. The SHOULD from Section 3 applies: servers SHOULD respond with `400`. Connection close and timeout are also acceptable since the server may not recognize this as a valid HTTP message at all.

### Scoring Justification

**Scored (SHOULD) -- Pass/Warn.** The RFC uses SHOULD for invalid request-line handling. Since `GET /\r\n` is clearly an invalid request-line (missing the mandatory HTTP-version), responding with `400` earns a Pass. A connection close or timeout is also acceptable because the server may be waiting for the rest of a partially received HTTP/1.x request-line. A server that processes this as an HTTP/0.9 request and returns content without headers would be a Warn, since it is serving a deprecated protocol with no framing.

### Edge Cases

- **No CRLF terminator:** A raw `GET /` with no line terminator may cause the server to wait indefinitely for the rest of the request-line, resulting in a timeout. This is acceptable behavior.
- **HTTP/0.9 POST:** `POST /\r\n` is even more dangerous than `GET /` because HTTP/0.9 had no concept of request bodies. A server that processes this may read subsequent data as a new request.
- **Pipeline after HTTP/0.9:** If an attacker sends `GET /\r\nGET /secret HTTP/1.1\r\n...`, a server that processes the first line as HTTP/0.9 and then reads the second as a new HTTP/1.1 request could be tricked into request smuggling.

## Sources

- [RFC 9112 Section 2.3 -- HTTP Version](https://www.rfc-editor.org/rfc/rfc9112#section-2.3)
- [RFC 9112 Section 3 -- Request Line](https://www.rfc-editor.org/rfc/rfc9112#section-3)
