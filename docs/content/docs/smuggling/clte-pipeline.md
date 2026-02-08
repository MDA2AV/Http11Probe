---
title: "CLTE-PIPELINE"
description: "CLTE-PIPELINE test documentation"
weight: 8
---

| | |
|---|---|
| **Test ID** | `SMUG-CLTE-PIPELINE` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | MUST close connection |
| **Expected** | `400` or close |

## What it sends

A full CL.TE smuggling payload — a POST request with both Content-Length and Transfer-Encoding headers, where the body contains a chunked `0` terminator followed by a smuggled second request.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 4\r\n
Transfer-Encoding: chunked\r\n
\r\n
0\r\n
\r\n
```

Followed immediately on the same connection by:

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

A CL-only parser reads 4 bytes (`0\r\n\r`) as the body and sees the follow-up `GET`. A TE parser sees the `0` chunk as end-of-body and processes the `GET` as a separate request.


## What the RFC says

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling (Section 11.2) or response splitting (Section 11.1) and ought to be handled as an error." — RFC 9112 §6.3

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks." — RFC 9112 §6.1

## Why it matters

This is not a theoretical test — it's the actual attack payload. If the server processes the first request using CL and the second appears in the pipeline, the smuggling succeeded.

## Deep Analysis

### Relevant ABNF

From RFC 9112 Section 6, the message body length algorithm depends on these headers:

```
Transfer-Encoding = #transfer-coding
Content-Length    = 1*DIGIT
message-body     = *OCTET
```

When both headers are present, the specification defines a strict precedence rule that eliminates ambiguity.

### RFC Evidence

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling or response splitting and ought to be handled as an error." -- RFC 9112 Section 6.3

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks." -- RFC 9112 Section 6.1

> "A sender MUST NOT send a Content-Length header field in any message that contains a Transfer-Encoding header field." -- RFC 9112 Section 6.2

### Chain of Reasoning

1. **Dual framing headers create ambiguity by design.** The CL.TE payload sends `Content-Length: 4` and `Transfer-Encoding: chunked` simultaneously. A CL-only parser reads exactly 4 bytes (`0\r\n\r`) as the body, leaving the trailing `\n` and the pipelined `GET` request on the connection. A TE-compliant parser reads the `0` chunk terminator and considers the body complete at a different boundary.

2. **RFC 9112 Section 6.3 explicitly names this as a smuggling vector.** The specification does not merely discourage dual headers -- it calls out "request smuggling" by name and says the message "ought to be handled as an error." This is one of the rare cases where the RFC specifically names the attack it is trying to prevent.

3. **The MUST-close requirement in Section 6.1 is the critical defense.** Even if the server chooses to process the request (using TE alone, as permitted), it MUST close the connection afterward. This prevents any leftover bytes from being interpreted as a subsequent request. A server that keeps the connection open after processing a dual-header request is vulnerable regardless of which framing method it chose.

4. **Attack scenario.** An attacker sends the CL.TE payload to a proxy-origin pair. The proxy uses Content-Length (reads 4 bytes, forwards, then reads the `GET` as a separate pipelined request). The origin uses Transfer-Encoding (reads the `0` chunk, considers the POST done, then reads the `GET` -- but from the attacker's smuggled bytes, not from the proxy's pipeline). The origin now processes a request the proxy never authorized, potentially with attacker-controlled headers and path.

### Scored / Unscored Justification

This test is **scored** -- a `2xx` response results in a **Fail**. The RFC uses MUST-level language requiring connection closure after processing a dual CL+TE request, and the specification explicitly identifies this pattern as a smuggling vector. The test sends a pipelined follow-up `GET` on the same connection: if the server responds to it with `2xx`, it means the connection was kept alive after the ambiguous request, directly violating the MUST-close requirement and demonstrating exploitable behavior. There is no RFC-defensible reason for a server to keep the connection open in this scenario.

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
- [PortSwigger — HTTP Request Smuggling](https://portswigger.net/web-security/request-smuggling)
