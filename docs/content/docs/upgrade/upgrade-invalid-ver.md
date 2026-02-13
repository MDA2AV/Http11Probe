---
title: "UPGRADE-INVALID-VER"
description: "UPGRADE-INVALID-VER test documentation"
weight: 4
---

| | |
|---|---|
| **Test ID** | `COMP-UPGRADE-INVALID-VER` |
| **Category** | Compliance |
| **RFC** | [RFC 6455 Section 4.4](https://www.rfc-editor.org/rfc/rfc6455#section-4.4) |
| **Requirement** | MUST abort handshake (426 preferred) |
| **Expected** | `426` or non-`101` |

## What it sends

A valid WebSocket upgrade request with `Sec-WebSocket-Version: 99` — a version the server does not support.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Connection: Upgrade\r\n
Upgrade: websocket\r\n
Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n
Sec-WebSocket-Version: 99\r\n
\r\n
```

WebSocket version `99` is not a valid version (only `13` is standard).


## What the RFC says

> "If this version does not match a version understood by the server, the server MUST abort the WebSocket handshake described in this section and instead send an appropriate HTTP error code (such as 426 Upgrade Required) and a |Sec-WebSocket-Version| header field indicating the version(s) the server is capable of understanding." -- RFC 6455 Section 4.2.2

> "If the server doesn't support the requested version, it MUST respond with a |Sec-WebSocket-Version| header field (or multiple |Sec-WebSocket-Version| header fields) containing all versions it is willing to use." -- RFC 6455 Section 4.4

## Why it matters

A server that returns `101` for an unsupported WebSocket version would enter an undefined protocol state. The ideal response is `426` with a `Sec-WebSocket-Version` header listing supported versions. Servers that don't support WebSocket at all will return 2xx (ignoring the upgrade), which is acceptable but noted as a warning.

## Deep Analysis

### Relevant ABNF Grammar

```
; From RFC 9110 Section 7.8:
Upgrade          = 1#protocol
protocol         = protocol-name [ "/" protocol-version ]
protocol-name    = token
protocol-version = token
```

The WebSocket version is communicated via `Sec-WebSocket-Version`, not within the Upgrade header's protocol-version component. The only standardized WebSocket version is 13 (defined in RFC 6455).

### RFC Evidence

**RFC 6455 Section 4.2.2** mandates version validation by the server:

> "If this version does not match a version understood by the server, the server MUST abort the WebSocket handshake described in this section and instead send an appropriate HTTP error code (such as 426 Upgrade Required) and a |Sec-WebSocket-Version| header field indicating the version(s) the server is capable of understanding." -- RFC 6455 Section 4.2.2

**RFC 6455 Section 4.4** reinforces the version negotiation requirement:

> "If the server doesn't support the requested version, it MUST respond with a |Sec-WebSocket-Version| header field (or multiple |Sec-WebSocket-Version| header fields) containing all versions it is willing to use." -- RFC 6455 Section 4.4

**RFC 6455 Section 4.2** establishes the required version value:

> "A |Sec-WebSocket-Version| header field, with a value of 13." -- RFC 6455 Section 4.2

### Chain of Reasoning

1. The test sends `Sec-WebSocket-Version: 99` -- a version that no server supports (only version 13 is standardized).
2. RFC 6455 Section 4.2.2 requires the server to abort the WebSocket handshake and return an error code (suggesting 426) with a `Sec-WebSocket-Version` header listing supported versions.
3. The MUST in Section 4.2.2 is clear: the server must abort and send an error. The "such as 426" phrasing suggests 426 is the preferred but not the only acceptable error code.
4. Servers that do not implement WebSocket at all will ignore the Upgrade header and return 2xx (processing the GET normally). This is acceptable because the server is not entering a WebSocket protocol state.
5. A 101 response for version 99 is a critical failure: the server is claiming to switch to WebSocket version 99, which does not exist. The connection enters an undefined state.

### Scoring Justification

**Scored (MUST).** RFC 6455 Section 4.2.2 uses MUST for aborting the handshake on unsupported versions. The ideal response is 426 with `Sec-WebSocket-Version: 13`. Any non-101 response is acceptable (including 2xx from servers that do not support WebSocket). Only 101 is a fail, as it represents switching to an unsupported protocol version. The SHOULD-level preference for 426 specifically means that other 4xx codes are also passing but less informative.

## Sources

- [RFC 6455 Section 4.2.2](https://www.rfc-editor.org/rfc/rfc6455#section-4.2.2)
- [RFC 6455 Section 4.4](https://www.rfc-editor.org/rfc/rfc6455#section-4.4)
