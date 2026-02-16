---
title: "CL-TE-BOTH"
description: "CL-TE-BOTH test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `SMUG-CL-TE-BOTH` |
| **Category** | Smuggling |
| **RFC** | [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | "ought to" handle as error |
| **Expected** | `400` or `2xx` |

## What it sends

A request with both `Content-Length` and `Transfer-Encoding` headers present.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 23\r\n
Transfer-Encoding: chunked\r\n
\r\n
D\r\n
hello-bananas\r\n
0\r\n
\r\n
```


## What the RFC says

RFC 9112 §6.3 states:

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling (Section 11.2) or response splitting (Section 11.1) and ought to be handled as an error."

RFC 9112 §6.1 provides the server's options:

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks."

The "ought to" language is between SHOULD and MAY. A server MAY reject the message or process it using Transfer-Encoding alone -- both are RFC-compliant. However, the server MUST close the connection afterward.

## Pass / Warn

The RFC uses "ought to" language (between SHOULD and MAY) for handling this as an error, and explicitly allows the server to either reject or process with TE alone. Both `400` and `2xx` are RFC-compliant, but rejection is the safer choice.

**Pass:** Server rejects with `400` (strict, safe).
**Warn:** Server accepts and responds `2xx` (RFC-valid, using TE to determine body length).

## Why it matters

This is **the** classic request smuggling setup. If the front-end uses Content-Length and the back-end uses Transfer-Encoding (or vice versa), they disagree on body boundaries.

## Deep Analysis

### ABNF Analysis

This test is not about an ABNF grammar violation. Both `Content-Length: 23` and `Transfer-Encoding: chunked` are individually valid headers. The issue is their simultaneous presence in the same message, which the RFC treats as a conflicting-framing condition.

### RFC Evidence Chain

**Step 1 -- The sender is prohibited from including both.**

> "A sender MUST NOT send a Content-Length header field in any message that contains a Transfer-Encoding header field." -- RFC 9112 §6.2

The request violates this MUST NOT at the sender level. When a server receives such a message, it knows the sender has already violated the protocol.

**Step 2 -- The RFC flags this as a potential attack.**

> "If a message is received with both a Transfer-Encoding and a Content-Length header field, the Transfer-Encoding overrides the Content-Length. Such a message might indicate an attempt to perform request smuggling (Section 11.2) or response splitting (Section 11.1) and ought to be handled as an error." -- RFC 9112 §6.3

The "ought to" language recommends treating this as an error but does not mandate it.

**Step 3 -- The server has explicit discretion.**

> "A server MAY reject a request that contains both Content-Length and Transfer-Encoding or process such a request in accordance with the Transfer-Encoding alone. Regardless, the server MUST close the connection after responding to such a request to avoid the potential attacks." -- RFC 9112 §6.1

The server has two compliant options: reject (400) or process using Transfer-Encoding only. In either case, it MUST close the connection afterward.

### Scored / Unscored Justification

This test is scored as **"ought to" handle as error** (Pass for 400, Warn for 2xx). The RFC uses "ought to" language -- stronger than MAY but weaker than MUST. The explicit MAY in RFC 9112 §6.1 permits both behaviors. However, the mandatory connection close after any response means a 2xx without connection close would be a separate violation.

### Real-World Smuggling Scenario

This is the classic CL.TE / TE.CL smuggling setup described by Watchfire (2005) and popularized by PortSwigger (2019). If different components disagree on whether to use `Content-Length` or `Transfer-Encoding`, they can disagree on request body boundaries or request body content. In real-world smuggling exploits, the attacker typically chooses a conflicting `Content-Length` such that one parser stops early and the remaining bytes are interpreted as the start of the next request on a persistent connection. This test uses a non-empty chunked body (`hello-bananas`) to make it obvious whether an echo-capable server decoded chunked framing (TE) or treated the raw chunked bytes as the body (CL).

## Sources

- [RFC 9112 §6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
- [RFC 9112 §6.2](https://www.rfc-editor.org/rfc/rfc9112#section-6.2)
- [RFC 9112 §6.3](https://www.rfc-editor.org/rfc/rfc9112#section-6.3)
