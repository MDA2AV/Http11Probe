---
title: "UPGRADE-POST"
description: "UPGRADE-POST test documentation"
weight: 1
---

| | |
|---|---|
| **Test ID** | `COMP-UPGRADE-POST` |
| **Category** | Compliance |
| **RFC** | [RFC 6455 Section 4.1](https://www.rfc-editor.org/rfc/rfc6455#section-4.1) |
| **Requirement** | MUST use GET |
| **Expected** | any non-`101` response |

## What it sends

A WebSocket upgrade request using `POST` instead of `GET`, with all standard WebSocket headers (`Connection: Upgrade`, `Upgrade: websocket`, `Sec-WebSocket-Key`, `Sec-WebSocket-Version: 13`).

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Connection: Upgrade\r\n
Upgrade: websocket\r\n
Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n
Sec-WebSocket-Version: 13\r\n
\r\n
```


## What the RFC says

> "The method of the request MUST be GET, and the HTTP version MUST be at least 1.1." -- RFC 6455 Section 4.1

The server-side validation reinforces this:

> "The client's opening handshake consists of the following parts. If the server, while reading the handshake, finds that the client did not send a handshake that matches the description below... the server MUST stop processing the client's handshake and return an HTTP response with an appropriate error code (such as 400 Bad Request)." -- RFC 6455 Section 4.2.1

> "1. An HTTP/1.1 or higher GET request" -- RFC 6455 Section 4.2.1

The opening handshake is defined exclusively for GET. A server that accepts an upgrade via POST is violating the WebSocket protocol.

## Why it matters

Allowing WebSocket upgrades on non-GET methods expands the attack surface. POST-based upgrades could bypass CSRF protections or WAF rules that only inspect GET-based WebSocket handshakes.

## Deep Analysis

### Relevant ABNF Grammar

```
; From RFC 9110 Section 7.8:
Upgrade          = 1#protocol
protocol         = protocol-name [ "/" protocol-version ]
protocol-name    = token
protocol-version = token
```

The Upgrade header grammar defines the protocol negotiation syntax, but the WebSocket-specific requirement comes from RFC 6455, which restricts the opening handshake to the GET method only.

### RFC Evidence

**RFC 6455 Section 4.1** mandates the method:

> "The method of the request MUST be GET, and the HTTP version MUST be at least 1.1." -- RFC 6455 Section 4.1

**RFC 6455 Section 4.2** mandates server-side validation:

> "The server MUST stop processing the client's handshake and return an HTTP response with an appropriate error code (such as 400 Bad Request)" -- RFC 6455 Section 4.2

**RFC 6455 Section 4.2** specifies the expected structure:

> "An HTTP/1.1 or higher GET request, including a 'Request-URI'...that should be interpreted as a /resource name/" -- RFC 6455 Section 4.2

### Chain of Reasoning

1. The test sends a WebSocket upgrade request using POST instead of GET. All other WebSocket headers are correct (`Connection: Upgrade`, `Upgrade: websocket`, `Sec-WebSocket-Key`, `Sec-WebSocket-Version: 13`).
2. RFC 6455 Section 4.1 uses MUST for the GET method requirement. The opening handshake is exclusively defined for GET.
3. On the server side, RFC 6455 Section 4.2 requires the server to validate that the request is "An HTTP/1.1 or higher GET request." If validation fails, the server "MUST stop processing the client's handshake and return an HTTP response with an appropriate error code."
4. The test expects any non-101 response. A 400 (Bad Request) or 405 (Method Not Allowed) would be ideal, but even a 200 (ignoring the upgrade entirely) is acceptable because it means the server did not switch protocols.
5. A 101 response to a POST-based upgrade is a compliance failure -- the server entered the WebSocket protocol through an unauthorized method, potentially bypassing WAF rules and CSRF protections that only inspect GET-based handshakes.

### Scoring Justification

**Scored (MUST).** RFC 6455 requires GET for the WebSocket opening handshake. Any non-101 response is a pass because it means the server did not incorrectly switch protocols. A 101 response is a fail because the server accepted a WebSocket upgrade via an unauthorized method. Servers that do not support WebSocket at all will naturally pass this test by returning 2xx or 4xx for the POST request.

## Sources

- [RFC 6455 Section 4.1](https://www.rfc-editor.org/rfc/rfc6455#section-4.1)
