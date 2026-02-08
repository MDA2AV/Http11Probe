---
title: "EXPECT-100-CL"
description: "EXPECT-100-CL test documentation"
weight: 33
---

| | |
|---|---|
| **Test ID** | `SMUG-EXPECT-100-CL` |
| **Category** | Smuggling |
| **RFC** | [RFC 9110 §10.1.1](https://www.rfc-editor.org/rfc/rfc9110#section-10.1.1) |
| **Requirement** | Unscored |
| **Expected** | `400` or `2xx` |

## What it sends

POST with `Content-Length: 5` and `Expect: 100-continue`, body included immediately.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 5\r\n
Expect: 100-continue\r\n
\r\n
hello
```

The body is sent immediately without waiting for a `100 Continue` response.


## What the RFC says

> "Upon receiving an HTTP/1.1 (or later) request that has a method, target URI, and complete header section that contains a 100-continue expectation and an indication that request content will follow, an origin server MUST send either: an immediate response with a final status code, if that status can be determined by examining just the method, target URI, and header fields, or an immediate 100 (Continue) response to encourage the client to send the request content." — RFC 9110 §10.1.1

> "A server MAY omit sending a 100 (Continue) response if it has already received some or all of the content for the corresponding request, or if the framing indicates that there is no content." — RFC 9110 §10.1.1

## Why this test is unscored

The RFC requires the server to send either a `100 Continue` interim response or a final status code when it receives `Expect: 100-continue`. However, the client in this test sends the body immediately without waiting. The server may still process the body normally (responding `2xx`), or it may reject the request. Both behaviors are implementation-dependent and valid.

**Pass:** Server rejects with `400` (strict, safe).
**Warn:** Server accepts and responds `2xx` (processes body despite Expect header).

## Why it matters

This test checks whether the server properly handles the body stream when `Expect: 100-continue` is present but the body arrives immediately. If the server ignores the body because it was waiting to send `100 Continue`, the body bytes remain on the connection and can be misinterpreted as the next request -- a connection desync.

## Deep Analysis

### RFC Evidence

> "Upon receiving an HTTP/1.1 (or later) request that has a method, target URI, and complete header section that contains a 100-continue expectation and an indication that request content will follow, an origin server MUST send either: an immediate response with a final status code, if that status can be determined by examining just the method, target URI, and header fields, or an immediate 100 (Continue) response to encourage the client to send the request content." -- RFC 9110 Section 10.1.1

> "A server MAY omit sending a 100 (Continue) response if it has already received some or all of the content for the corresponding request, or if the framing indicates that there is no content." -- RFC 9110 Section 10.1.1

> "A server that responds with a final status code before reading the entire request content SHOULD indicate in that response whether it intends to close the connection or continue reading and discarding the request content." -- RFC 9110 Section 10.1.1

### Chain of Reasoning

1. **The Expect mechanism assumes a client-server handshake.** The normal flow is: client sends headers with `Expect: 100-continue`, waits, server sends `100 Continue` or a final status, then the client sends (or does not send) the body. This test breaks that assumption by sending the body immediately alongside the headers, without waiting for any server response.

2. **The RFC explicitly permits the server to skip the 100 response.** Section 10.1.1 says the server MAY omit the `100 Continue` response if it has already received the content. This means the server is allowed to silently process the body that arrived early. However, the server might also have internal state that expects to "gate" the body read behind the 100-continue handshake.

3. **The desync occurs when the server does not read the body.** If the server's `Expect: 100-continue` handling causes it to respond with a final status (e.g., `200 OK`) before reading the 5 bytes declared by Content-Length, those 5 bytes (`hello`) remain on the TCP connection. On a persistent connection, the server will attempt to parse `hello` as the start of the next HTTP request -- interpreting `hello` as a malformed request method and potentially entering an undefined state.

4. **Attack scenario.** An attacker targets a server that gates body reads behind `Expect: 100-continue`. The attacker sends `POST / HTTP/1.1` with `Expect: 100-continue`, `Content-Length: N`, and a crafted body containing a smuggled HTTP request. The server responds with `200 OK` without reading the body. The smuggled request bytes sit on the connection and are parsed as a new request -- one that the server processes with the attacker's chosen method, path, and headers.

### Scored / Unscored Justification

This test is **unscored** (`Scored = false`). The RFC permits the server to omit the `100 Continue` response if it has already received the content, and it also permits the server to respond with a final status before reading the body. Both `400` (rejecting the request outright) and `2xx` (processing the body normally) are defensible behaviors. The critical question -- whether the server leaves unread bytes on the connection -- cannot be determined from the status code alone, which is why this test flags `2xx` as a warning rather than a failure. A `2xx` response may indicate correct body consumption, or it may indicate a desync; the test surfaces the behavior for manual investigation.

## Sources

- [RFC 9110 §10.1.1](https://www.rfc-editor.org/rfc/rfc9110#section-10.1.1)
