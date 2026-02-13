---
title: Connections
description: "TCP connection lifecycle, persistent connections, pipelining, Upgrade, and 100 Continue."
weight: 4
---

HTTP/1.1 runs over TCP (or TLS over TCP for HTTPS). This page covers the connection lifecycle and the features HTTP/1.1 provides for efficient connection use.

## TCP Connection Lifecycle

A typical HTTP/1.1 exchange:

1. **DNS resolution** — the client resolves the server's hostname to an IP address. May involve multiple queries (A, AAAA, CNAME).
2. **TCP handshake** — a three-way handshake (SYN → SYN-ACK → ACK) establishes the connection. Adds one round-trip of latency.
3. **TLS handshake** (if HTTPS) — client and server negotiate cipher suites, exchange certificates, and derive session keys. TLS 1.2 adds two round-trips; TLS 1.3 adds one (or zero with 0-RTT resumption).
4. **Request/response exchange** — the client sends one or more requests; the server responds in order.
5. **Connection close** — either side sends `Connection: close`, the TCP connection times out, or a TCP RST is sent.

Before any HTTP data can flow, the overhead is at minimum one round-trip (TCP) and often two or three (TCP + TLS). This is why connection reuse matters.

## Persistent Connections (Keep-Alive)

One of HTTP/1.1's most important improvements over 1.0 is **persistent connections**.

### How It Changed

- In **HTTP/1.0**, every request required a new TCP connection. Three-way handshake, slow-start, optional TLS negotiation — all repeated for every request. This added hundreds of milliseconds of latency per resource.
- In **HTTP/1.1**, connections are **persistent by default**. Multiple requests and responses can be sent sequentially over the same TCP connection without renegotiating.

### Wire Example

```http
GET /page1 HTTP/1.1
Host: example.com

HTTP/1.1 200 OK
Content-Length: 500

...body...

GET /style.css HTTP/1.1
Host: example.com

HTTP/1.1 200 OK
Content-Length: 300

...body...

GET /script.js HTTP/1.1
Host: example.com
Connection: close

HTTP/1.1 200 OK
Content-Length: 800

...body...
(TCP connection closed)
```

Three requests over one connection. The third request includes `Connection: close` to signal that the connection should be closed after the response.

### Benefits

- **Eliminates TCP handshake overhead** for subsequent requests.
- **TCP congestion window grows** over the life of the connection, improving throughput for later requests.
- **Reduces server resource usage** — fewer sockets, fewer TIME_WAIT entries, less memory.
- **Enables pipelining** (see below).

### Closing a Connection

Either side can close the connection:

- **`Connection: close`** — the sender will close the connection after this message. The recipient should not send further requests on this connection.
- **Server timeout** — most servers close idle connections after a configurable period (e.g., 60 seconds in Nginx, 5 seconds in Apache).
- **TCP RST** — abrupt connection termination. Can happen if the server crashes, hits a resource limit, or detects a protocol error.

## Pipelining

HTTP/1.1 allows **pipelining** — sending multiple requests without waiting for each response:

```
Client → Server:  GET /a    GET /b    GET /c
Server → Client:  resp /a   resp /b   resp /c
```

Responses **MUST** be returned in the same order as the requests. This creates **head-of-line (HOL) blocking**: if `/a` is slow (e.g., a large database query), `/b` and `/c` are delayed even if they're ready.

### Why Pipelining Failed

In practice, pipelining is rarely used:

- **HOL blocking** negates most latency benefits.
- **Buggy intermediaries** — many proxies and load balancers don't handle pipelined requests correctly, sometimes sending responses out of order or dropping requests.
- **Error recovery is complex** — if the connection drops mid-pipeline, the client doesn't know which requests were processed.
- **Browsers never enabled it** — no major browser ships with pipelining on by default.

HTTP/2's **multiplexing** solves this by allowing interleaved responses on independent streams.

## Connection Management Headers

### `Connection`

The `Connection` header serves two purposes:

1. **Signaling connection close** — `Connection: close` tells the other side the connection will be closed after this message.
2. **Listing hop-by-hop headers** — any headers listed in `Connection` are hop-by-hop and MUST be removed by proxies before forwarding. For example, `Connection: Keep-Alive, X-Custom` means both `Keep-Alive` and `X-Custom` are consumed by the next hop.

### `Keep-Alive`

The `Keep-Alive` header is informational and can suggest parameters:

```http
Keep-Alive: timeout=5, max=100
```

- `timeout` — how many seconds the server will keep the idle connection open.
- `max` — maximum number of requests the server will accept on this connection.

These values are **not binding** — either side can close at any time.

## Protocol Upgrade

The `Upgrade` header allows switching from HTTP/1.1 to a different protocol on the same connection.

### Mechanism

1. Client sends a request with `Upgrade` and `Connection: Upgrade`:

```http
GET /chat HTTP/1.1
Host: example.com
Upgrade: websocket
Connection: Upgrade
Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==
Sec-WebSocket-Version: 13
```

2. If the server agrees, it responds with `101 Switching Protocols`:

```http
HTTP/1.1 101 Switching Protocols
Upgrade: websocket
Connection: Upgrade
Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=
```

3. From this point, the connection speaks the new protocol (WebSocket in this example).

### Common Upgrades

| Target Protocol | Usage |
|----------------|-------|
| WebSocket | Full-duplex communication for real-time applications. |
| h2c | HTTP/2 over cleartext (no TLS). Rarely used — most HTTP/2 uses ALPN during TLS. |
| TLS/1.0 | Historical — upgrading an HTTP connection to HTTPS (largely replaced by direct HTTPS). |

## 100 Continue

The `Expect: 100-continue` mechanism prevents clients from sending large request bodies that the server will reject.

### Flow

1. Client sends headers with `Expect: 100-continue` but **withholds the body**:

```http
POST /upload HTTP/1.1
Host: example.com
Content-Length: 52428800
Expect: 100-continue
```

2. Server checks the headers (authentication, content-length limits, etc.):
   - If acceptable: responds with `100 Continue` — the client then sends the body.
   - If not acceptable: responds with a 4xx error (e.g., `413 Content Too Large`) — the client never sends the 50MB body.

```http
HTTP/1.1 100 Continue

(client sends body)

HTTP/1.1 200 OK
```

### Why It Matters

Without `Expect: 100-continue`, a client uploading a large file would send the entire body before learning the server rejects it (wrong auth, too large, wrong content type). This wastes bandwidth and time.

## Line Endings and Parsing Strictness

RFC 9112 §2.2 defines strict rules for line endings in HTTP/1.1 messages:

- All line endings **MUST** be CRLF (`\r\n`).
- Bare CR (`\r`) without a following LF is **not a valid line terminator** and MUST be rejected.
- Bare LF (`\n`) without a preceding CR — the spec says a server **MAY** accept bare LF as a line terminator in the request-line and header fields, but this is a robustness concession, not a requirement.

These rules exist to prevent parsing ambiguities. If a front-end proxy interprets line endings differently from a back-end server, an attacker can exploit the discrepancy for **request smuggling** or **header injection**.
