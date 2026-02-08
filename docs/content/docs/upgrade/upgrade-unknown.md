---
title: "UPGRADE-UNKNOWN"
description: "UPGRADE-UNKNOWN test documentation"
weight: 3
---

| | |
|---|---|
| **Test ID** | `COMP-UPGRADE-UNKNOWN` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 Section 7.8](https://www.rfc-editor.org/rfc/rfc9110#section-7.8) |
| **Requirement** | MUST NOT switch |
| **Expected** | any non-`101` response |

## What it sends

A GET request with `Connection: Upgrade` and `Upgrade: totally-made-up/1.0` — a protocol the server cannot possibly support.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Connection: Upgrade\r\n
Upgrade: totally-made-up/1.0\r\n
\r\n
```


## What the RFC says

> "A server MAY ignore a received Upgrade header field if it wishes to continue using the current protocol on that connection. Upgrade cannot be used to insist on a protocol change." -- RFC 9110 Section 7.8

> "A server MUST NOT switch to a protocol that was not indicated by the client in the corresponding request's Upgrade header field." -- RFC 9110 Section 7.8

> "A server MUST NOT switch protocols unless the received message semantics can be honored by the new protocol; an OPTIONS request can be honored by any protocol." -- RFC 9110 Section 7.8

A server must not respond with `101 Switching Protocols` for a protocol it does not implement. It should ignore the Upgrade header and process the request normally (2xx) or reject it.

## Why it matters

A server that blindly returns `101` for any Upgrade value is broken — it switches the connection to an undefined state, potentially leaving the TCP stream in a desynced state exploitable for smuggling.

## Deep Analysis

### Relevant ABNF Grammar

```
Upgrade          = 1#protocol
protocol         = protocol-name [ "/" protocol-version ]
protocol-name    = token
protocol-version = token
```

The Upgrade grammar accepts any `token "/" token` combination as a protocol identifier. The value `totally-made-up/1.0` is syntactically valid per this grammar -- it is a well-formed protocol name with a version. The issue is semantic: the server does not implement this protocol.

### RFC Evidence

**RFC 9110 Section 7.8** permits servers to ignore the Upgrade:

> "A server MAY ignore a received Upgrade header field if it wishes to continue using the current protocol on that connection. Upgrade cannot be used to insist on a protocol change." -- RFC 9110 Section 7.8

**RFC 9110 Section 7.8** prohibits switching to unlisted protocols:

> "A server MUST NOT switch to a protocol that was not indicated by the client in the corresponding request's Upgrade header field." -- RFC 9110 Section 7.8

**RFC 9110 Section 7.8** requires semantic compatibility:

> "A server MUST NOT switch protocols unless the received message semantics can be honored by the new protocol; an OPTIONS request can be honored by any protocol." -- RFC 9110 Section 7.8

### Chain of Reasoning

1. The test sends `Upgrade: totally-made-up/1.0` -- a protocol that no server implements.
2. The server has two compliant options: (a) ignore the Upgrade header and process the GET request normally (returning 2xx), or (b) return a non-101 error response.
3. A server MUST NOT respond with 101 for a protocol it does not implement. The 101 status code means "I am switching to the protocol you requested," which is impossible for an unknown protocol.
4. If a server blindly returns 101 for any Upgrade value, the TCP connection enters an undefined state. The client expects to speak the requested protocol, but the server has no implementation. This desync can be exploited for smuggling.
5. The MUST NOT in "A server MUST NOT switch to a protocol that was not indicated by the client" is technically about switching to a different protocol than requested. But the stronger implication is that a server cannot switch to a protocol it does not support at all.

### Scoring Justification

**Scored (MUST NOT).** A 101 response for an unknown protocol violates the MUST NOT requirements in RFC 9110 Section 7.8. Any non-101 response is a pass, including 2xx (server ignored the Upgrade and processed the GET normally) or 4xx (server rejected the request). Only 101 is a fail, as it indicates the server attempted to switch to a protocol it cannot support.

## Sources

- [RFC 9110 Section 7.8](https://www.rfc-editor.org/rfc/rfc9110#section-7.8)
