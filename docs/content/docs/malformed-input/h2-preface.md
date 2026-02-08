---
title: "H2-PREFACE"
description: "H2-PREFACE test documentation"
weight: 17
---

| | |
|---|---|
| **Test ID** | `MAL-H2-PREFACE` |
| **Category** | Malformed Input |
| **Expected** | `400`/`505`, close, or timeout |

## What it sends

The HTTP/2 connection preface (`PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n`) to an HTTP/1.1 server.

```http
PRI * HTTP/2.0\r\n
\r\n
SM\r\n
\r\n
```

The HTTP/2 connection preface, sent to an HTTP/1.1 server.


## What the RFC says

The HTTP/2 connection preface starts with the string `PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n`:

> "The client connection preface is selected so that a large proportion of HTTP/1.1 or HTTP/1.0 servers and intermediaries do not attempt to process further frames." — RFC 9113 Section 3.4

When parsed as HTTP/1.1, `PRI` is an unknown method and `HTTP/2.0` is an unsupported version. The request-line grammar is:

> `request-line = method SP request-target SP HTTP-version` — RFC 9112 Section 3

> `HTTP-version = HTTP-name "/" DIGIT "." DIGIT` — RFC 9112 Section 2.3

`HTTP/2.0` is a valid version string syntactically, but HTTP/2.0 is not HTTP/1.1, so 505 (HTTP Version Not Supported) is appropriate. Alternatively:

> "When a server listening only for HTTP request messages...receives a sequence of octets that does not match the HTTP-message grammar...the server SHOULD respond with a 400 (Bad Request) response and close the connection." — RFC 9112 Section 2.2

## Why it matters

An HTTP/1.1-only server receiving the H2 preface should recognize it is not a valid HTTP/1.1 request. Parsing it as HTTP/1.1 could lead to unexpected behavior. The server should reject with 400 or 505, close the connection, or timeout.

## Deep Analysis

### ABNF context

When an HTTP/1.1 server parses the H2 preface as an HTTP/1.1 request, it sees:

```
request-line   = method SP request-target SP HTTP-version
method         = token
token          = 1*tchar
HTTP-version   = HTTP-name "/" DIGIT "." DIGIT
HTTP-name      = %s"HTTP"
```

Parsing `PRI * HTTP/2.0\r\n`:
- `method` = `PRI` -- a valid token (all characters are `tchar`), but not a recognized HTTP method.
- `request-target` = `*` -- valid `asterisk-form`, typically only used with OPTIONS.
- `HTTP-version` = `HTTP/2.0` -- syntactically valid (`HTTP-name "/" DIGIT "." DIGIT`), but the major version `2` indicates HTTP/2, not HTTP/1.1.

After the blank line (`\r\n`), the server encounters `SM\r\n\r\n` -- which looks like a truncated or malformed follow-up that does not match any HTTP/1.1 grammar production.

### RFC evidence

> "The client connection preface is selected so that a large proportion of HTTP/1.1 or HTTP/1.0 servers and intermediaries do not attempt to process further frames." -- RFC 9113 Section 3.4

> "This sequence MUST be followed by a SETTINGS frame (Section 6.5), which MAY be empty." -- RFC 9113 Section 3.4

> "Clients and servers MUST treat an invalid connection preface as a connection error of type PROTOCOL_ERROR." -- RFC 9113 Section 3.4

> "A server that receives a method longer than any that it implements SHOULD respond with a 501 (Not Implemented) status code." -- RFC 9112 Section 3

> "When a server listening only for HTTP request messages, or processing what appears from the start-line to be an HTTP request message, receives a sequence of octets that does not match the HTTP-message grammar aside from the robustness exceptions listed above, the server SHOULD respond with a 400 (Bad Request) response and close the connection." -- RFC 9112 Section 2.2

The H2 preface was deliberately designed to cause HTTP/1.1 servers to reject it. The `PRI` method is not a standard HTTP method, `*` as a request-target is only valid with OPTIONS, and `HTTP/2.0` signals an unsupported major version.

### Chain of reasoning

1. The server receives `PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n` on an HTTP/1.1 connection.
2. It parses the request-line: `PRI` (method), `*` (request-target), `HTTP/2.0` (version).
3. Multiple rejection paths exist:
   - **505 (HTTP Version Not Supported)**: The major version `2` indicates HTTP/2. An HTTP/1.1-only server does not support this version and may respond with 505.
   - **501 (Not Implemented)**: `PRI` is not a recognized HTTP method. Per RFC 9112 Section 3, the server SHOULD respond with 501.
   - **400 (Bad Request)**: After the empty line, `SM\r\n\r\n` does not constitute a valid HTTP/1.1 request or response. The overall byte sequence does not match the `HTTP-message` grammar when considered holistically.
   - **Connection close or timeout**: The server may simply close the connection or wait for data it can parse.
4. The H2 preface was intentionally designed to trigger these rejection paths in HTTP/1.1 servers, ensuring that an HTTP/2 client connecting to an HTTP/1.1-only server fails cleanly rather than producing protocol confusion.

### Security implications

- **Protocol confusion**: If an HTTP/1.1 server does not reject the H2 preface and instead attempts to process `SM` as a second request or body data, it may enter an undefined state. The binary HTTP/2 frames that follow the preface would be misinterpreted as HTTP/1.1 data, potentially causing crashes or exploitable behavior.
- **Downgrade detection**: An attacker probing whether a server supports HTTP/2 can send the H2 preface. A server that responds with 400/505 confirms it is HTTP/1.1-only; a server that upgrades to HTTP/2 confirms dual-protocol support. This fingerprinting aids in targeted attacks.
- **Proxy confusion**: If a front-end proxy speaks HTTP/2 and a backend only speaks HTTP/1.1, sending the H2 preface directly to the backend (bypassing the proxy's protocol translation) could trigger unexpected behavior. The backend must cleanly reject it.
- **Frame injection**: If the server somehow processes past the preface, the binary HTTP/2 SETTINGS frame that follows could be misinterpreted as HTTP/1.1 content, injecting controlled bytes into the server's parsing state.

## Sources

- [RFC 9113 Section 3.4](https://www.rfc-editor.org/rfc/rfc9113#section-3.4) — HTTP/2 connection preface
- [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) — rejection of invalid messages
- [RFC 9112 Section 3](https://www.rfc-editor.org/rfc/rfc9112#section-3) — request-line grammar
