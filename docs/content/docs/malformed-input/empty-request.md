---
title: "EMPTY-REQUEST"
description: "EMPTY-REQUEST test documentation"
weight: 13
---

| | |
|---|---|
| **Test ID** | `MAL-EMPTY-REQUEST` |
| **Category** | Malformed Input |
| **Expected** | `400`, close, or timeout |

## What it sends

Zero bytes -- the TCP connection is established and then closed without sending any data.

```
(zero bytes — TCP connection opened, no data sent)
```


## What the RFC says

> "In the interest of robustness, a server that is expecting to receive and parse a request-line SHOULD ignore at least one empty line (CRLF) received prior to the request-line." — RFC 9112 Section 2.2

Zero bytes means no request-line was received at all. The server has no data to parse against the HTTP-message grammar.

> "When a server listening only for HTTP request messages...receives a sequence of octets that does not match the HTTP-message grammar...the server SHOULD respond with a 400 (Bad Request) response and close the connection." — RFC 9112 Section 2.2

## Why timeout is acceptable

The server has no indication that a request was even attempted. With zero bytes received, the server cannot distinguish between a slow client and a connection that will never send data.

## Deep Analysis

### ABNF violation

An HTTP/1.1 message requires a complete structure:

```
HTTP-message  = start-line CRLF
                *( field-line CRLF )
                CRLF
                [ message-body ]

start-line    = request-line / status-line
request-line  = method SP request-target SP HTTP-version
method        = token
token         = 1*tchar
```

Zero bytes means no `start-line` was received. The `method` production requires `1*tchar` -- at least one character. With no characters at all, the `HTTP-message` grammar cannot match even its first production. The message is not merely malformed; it is entirely absent.

### RFC evidence

> "In the interest of robustness, a server that is expecting to receive and parse a request-line SHOULD ignore at least one empty line (CRLF) received prior to the request-line." -- RFC 9112 Section 2.2

> "When a server listening only for HTTP request messages, or processing what appears from the start-line to be an HTTP request message, receives a sequence of octets that does not match the HTTP-message grammar aside from the robustness exceptions listed above, the server SHOULD respond with a 400 (Bad Request) response and close the connection." -- RFC 9112 Section 2.2

> "An HTTP/1.1 user agent MUST NOT preface or follow a request with an extra CRLF." -- RFC 9112 Section 2.2

The robustness exception allows the server to ignore leading CRLFs -- but zero bytes is not a CRLF. There is no data to ignore. The server is waiting for a `request-line` that never arrives.

### Chain of reasoning

1. The TCP three-way handshake completes successfully. The connection is established.
2. The server enters its read loop, expecting the first byte of a `request-line`.
3. Zero bytes arrive. The client closes its end of the connection (or the test tool disconnects).
4. The server detects the connection closure (EOF / FIN).
5. At this point, the server has received no data at all. The `HTTP-message` grammar cannot match zero bytes.
6. Three legitimate server responses exist:
   - **400 (Bad Request)**: The server recognizes that a connection was opened but no valid request was sent. It replies with 400 before closing.
   - **Connection close**: The server silently closes its side. No bytes were exchanged, so no response is necessary.
   - **Timeout**: The server never detects EOF (e.g., the client holds the connection open without sending). The server eventually times out waiting for the request-line.
7. All three outcomes are acceptable because the RFC's SHOULD-400 guidance applies to "a sequence of octets that does not match the HTTP-message grammar." Zero octets is arguably not even "a sequence" -- the server was never given anything to parse.

### Security implications

- **Slowloris / connection exhaustion**: An attacker opens thousands of TCP connections and sends zero bytes on each. If the server allocates per-connection resources (memory, file descriptors, thread pool slots) and waits indefinitely for data, the server's connection capacity is exhausted, denying service to legitimate clients.
- **Port scanning and fingerprinting**: Opening a connection and sending nothing, then observing whether the server responds with 400, closes immediately, or times out after a specific duration, reveals information about the server implementation and its timeout configuration.
- **Resource leak detection**: Servers that do not properly clean up connections with zero data may leak file descriptors or memory over time, eventually crashing under sustained connection-open attacks.

## Sources

- [RFC 9112 Section 2.2](https://www.rfc-editor.org/rfc/rfc9112#section-2.2) — message parsing robustness
