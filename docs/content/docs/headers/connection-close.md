---
title: "CONNECTION-CLOSE"
description: "CONNECTION-CLOSE test documentation"
weight: 6
---

| | |
|---|---|
| **Test ID** | `COMP-CONNECTION-CLOSE` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 §9.6](https://www.rfc-editor.org/rfc/rfc9112#section-9.6) |
| **Requirement** | MUST |
| **Expected** | `2xx` + connection closed |

## What it sends

A standard GET request with `Connection: close` indicating the client wants the server to close the connection after sending the response.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
Connection: close\r\n
\r\n
```

## What the RFC says

> "A server that receives a 'close' connection option MUST initiate closure of the connection (see below) after it sends the final response to the request that contained the 'close' connection option. The server SHOULD send a 'close' connection option in its final response on that connection. The server MUST NOT process any further requests received on that connection." -- RFC 9112 Section 9.6

The server must both respond successfully and close the TCP connection afterward. Responding with `2xx` but leaving the connection open violates this requirement.

## Why it matters

If a server ignores `Connection: close` and keeps the connection alive, a client may send a second request on what it believes is a new connection. In proxy environments, this can lead to response mismatch: the proxy believes the connection is closed and assigns it to a different client, who then receives the first client's response. Honoring `Connection: close` is essential for correct connection lifecycle management.

## Deep Analysis

### Relevant ABNF Grammar

```
Connection        = 1#connection-option
connection-option = token
```

The `close` token is a connection option sent within the `Connection` header field. When present, it signals that the sender wishes to close the connection after the current request/response exchange.

### RFC Evidence

**RFC 9112 Section 9.6** mandates the server behavior unambiguously:

> "A server that receives a 'close' connection option MUST initiate closure of the connection after it sends the final response to the request that contained the 'close' connection option." -- RFC 9112 Section 9.6

**RFC 9112 Section 9.6** further prohibits processing additional requests:

> "The server MUST NOT process any further requests received on that connection." -- RFC 9112 Section 9.6

**RFC 9112 Section 9.6** also recommends the server echo the close option:

> "The server SHOULD send a 'close' connection option in its final response on that connection." -- RFC 9112 Section 9.6

### Chain of Reasoning

1. The test sends a standard `GET / HTTP/1.1` with `Connection: close`, requesting the server close the connection after responding.
2. The server MUST respond to the request (returning a normal status code such as 2xx) and then close the TCP connection.
3. A server that responds with 2xx but leaves the connection open violates the MUST requirement in Section 9.6.
4. The test validates two things: (a) the server returns a successful response, and (b) the server actually closes the TCP connection afterward.
5. If the server keeps the connection alive, downstream components (especially proxies) may misroute subsequent data, as they expect the connection to be closed.

### Scoring Justification

**Scored (MUST).** The RFC uses MUST for connection closure after receiving the `close` option. The expected behavior is a 2xx response followed by TCP connection close. The test validates both the response status and the connection state. A server that responds successfully but does not close the connection fails this test.

## Sources

- [RFC 9112 Section 9.6 -- Tear-down](https://www.rfc-editor.org/rfc/rfc9112#section-9.6)
- [RFC 9110 Section 7.6.1 -- Connection](https://www.rfc-editor.org/rfc/rfc9110#section-7.6.1)
