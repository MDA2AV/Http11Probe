---
title: "UPGRADE-HTTP10"
description: "UPGRADE-HTTP10 test documentation"
weight: 5
---

| | |
|---|---|
| **Test ID** | `COMP-UPGRADE-HTTP10` |
| **Category** | Compliance |
| **Scored** | Yes |
| **RFC** | [RFC 9110 §7.8](https://www.rfc-editor.org/rfc/rfc9110#section-7.8) |
| **RFC Level** | MUST |
| **Expected** | Not `101` |

## What it sends

An HTTP/1.0 request with WebSocket upgrade headers. The server must ignore the Upgrade field because it was received in an HTTP/1.0 request.

```http
GET / HTTP/1.0\r\n
Host: localhost:8080\r\n
Connection: Upgrade\r\n
Upgrade: websocket\r\n
Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n
Sec-WebSocket-Version: 13\r\n
\r\n
```

## What the RFC says

> "A server that receives an Upgrade header field in an HTTP/1.0 request MUST ignore that Upgrade field." — RFC 9110 §7.8

The Upgrade mechanism is an HTTP/1.1 feature. An HTTP/1.0 client cannot participate in protocol switching, so the server must not attempt it.

## Why it matters

If a server processes an Upgrade from an HTTP/1.0 client and returns `101 Switching Protocols`, the client likely cannot handle the protocol switch. This could lead to connection corruption or security issues, especially if a proxy is involved that speaks HTTP/1.0 to the backend.

## Verdicts

- **Pass** — Server returns any status other than `101` (correctly ignored Upgrade)
- **Fail** — Server returns `101 Switching Protocols` (incorrectly upgraded an HTTP/1.0 request)

## Sources

- [RFC 9110 §7.8](https://www.rfc-editor.org/rfc/rfc9110#section-7.8)
