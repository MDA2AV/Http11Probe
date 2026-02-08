---
title: "UPGRADE-MISSING-CONN"
description: "UPGRADE-MISSING-CONN test documentation"
weight: 2
---

| | |
|---|---|
| **Test ID** | `COMP-UPGRADE-MISSING-CONN` |
| **Category** | Compliance |
| **RFC** | [RFC 9110 Section 7.8](https://www.rfc-editor.org/rfc/rfc9110#section-7.8) |
| **Requirement** | MUST NOT switch |
| **Expected** | any non-`101` response |

## What it sends

A GET request with `Upgrade: websocket` and WebSocket handshake headers, but **without** the required `Connection: Upgrade` header.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Upgrade: websocket\r\n
Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n
Sec-WebSocket-Version: 13\r\n
\r\n
```

No `Connection: Upgrade` header — the `Upgrade` header is present alone.


## What the RFC says

> "A sender of Upgrade MUST also send an 'Upgrade' connection option in the Connection header field (Section 7.6.1) to inform intermediaries not to forward this field." -- RFC 9110 Section 7.8

The WebSocket RFC reinforces this at the server side:

> "A |Connection| header field that includes the token 'Upgrade', treated as an ASCII case-insensitive value." -- RFC 6455 Section 4.2.1 (required handshake element #4)

> "If the server, while reading the handshake, finds that the client did not send a handshake that matches the description below... the server MUST stop processing the client's handshake and return an HTTP response with an appropriate error code (such as 400 Bad Request)." -- RFC 6455 Section 4.2.1

Without `Connection: Upgrade`, the Upgrade header is hop-by-hop metadata that intermediaries should strip. A server that switches protocol anyway could be tricked via proxies.

## Why it matters

If a server switches protocol without `Connection: Upgrade`, a proxy forwarding the request would not know the connection semantics changed. This can lead to connection desync and smuggling through WebSocket-unaware intermediaries.

## Deep Analysis

### Relevant ABNF Grammar

```
; From RFC 9110 Section 7.8:
Upgrade          = 1#protocol
protocol         = protocol-name [ "/" protocol-version ]
protocol-name    = token

; From RFC 9110 Section 7.6.1:
Connection        = 1#connection-option
connection-option = token
```

The Upgrade and Connection headers work in tandem. The Upgrade header names the target protocol, while the `Connection: Upgrade` token signals that the Upgrade header is hop-by-hop and should not be forwarded by intermediaries.

### RFC Evidence

**RFC 9110 Section 7.8** mandates the pairing of Upgrade with Connection:

> "A sender of Upgrade MUST also send an 'Upgrade' connection option in the Connection header field to inform intermediaries not to forward this field." -- RFC 9110 Section 7.8

**RFC 6455 Section 4.2** lists Connection as a required handshake element:

> "A |Connection| header field that includes the token 'Upgrade', treated as an ASCII case-insensitive value." -- RFC 6455 Section 4.2

**RFC 6455 Section 4.2** mandates rejection of incomplete handshakes:

> "If the server, while reading the handshake, finds that the client did not send a handshake that matches the description below...the server MUST stop processing the client's handshake and return an HTTP response with an appropriate error code (such as 400 Bad Request)." -- RFC 6455 Section 4.2

### Chain of Reasoning

1. The test sends `Upgrade: websocket` with all other WebSocket headers, but omits `Connection: Upgrade`.
2. RFC 9110 Section 7.8 requires senders to include `Connection: Upgrade` alongside the Upgrade header. Without it, the Upgrade header is technically malformed from the sender's perspective.
3. From the server's perspective, the absence of `Connection: Upgrade` means an intermediary may have already stripped the Upgrade header's hop-by-hop semantics. The server should treat the Upgrade header as if it were not present.
4. RFC 6455 Section 4.2 lists `Connection: Upgrade` as a required element of the WebSocket handshake. Without it, the handshake is incomplete and the server MUST stop processing it.
5. A server that switches protocol without `Connection: Upgrade` creates a dangerous situation: any proxy in the request chain would be unaware that the connection semantics have changed, leading to connection desynchronization and potential smuggling.

### Scoring Justification

**Scored (MUST).** Both RFC 9110 Section 7.8 (MUST send Connection: Upgrade) and RFC 6455 Section 4.2 (MUST stop processing incomplete handshakes) establish MUST-level requirements. Any non-101 response is a pass. A 101 response is a fail because the server switched protocols without the required `Connection: Upgrade` header, violating both the HTTP and WebSocket specifications.

## Sources

- [RFC 9110 Section 7.8](https://www.rfc-editor.org/rfc/rfc9110#section-7.8)
- [RFC 6455 Section 4.1](https://www.rfc-editor.org/rfc/rfc6455#section-4.1)
