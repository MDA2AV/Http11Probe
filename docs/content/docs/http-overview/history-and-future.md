---
title: History and Future
description: "HTTP's evolution from 0.9 to 3, the current IETF work, alternatives to HTTP, and learning resources."
weight: 7
---

## History

| Year | Version | Key Milestone |
|------|---------|---------------|
| 1991 | HTTP/0.9 | Tim Berners-Lee's original protocol. Single-line `GET` request, HTML-only response, no headers, no status codes. |
| 1996 | HTTP/1.0 (RFC 1945) | Added headers, status codes, content types, and `POST`/`HEAD` methods. One request per TCP connection. |
| 1997 | HTTP/1.1 (RFC 2068) | Persistent connections, `Host` header (virtual hosting), chunked encoding, content negotiation. |
| 1999 | HTTP/1.1 (RFC 2616) | Consolidated and revised specification. The reference for over a decade. |
| 2014 | HTTP/1.1 (RFC 7230–7235) | Split into six focused documents, clarified edge cases, obsoleted RFC 2616. |
| 2022 | HTTP (RFC 9110/9112) | Current standard. Separated semantics (9110) from message syntax (9112). Version-agnostic semantics. |

### HTTP/0.9 (1991)

The original protocol had no version number, no headers, and no status codes. A request was a single line:

```
GET /page.html
```

The server responded with raw HTML and closed the connection. That's it. No content type, no error handling, no metadata.

### HTTP/1.0 (1996)

HTTP/1.0 (RFC 1945) added the features we now consider essential:

- **Headers** — both request and response headers for metadata.
- **Status codes** — `200 OK`, `404 Not Found`, `500 Internal Server Error`.
- **Content types** — the `Content-Type` header, enabling non-HTML responses.
- **New methods** — `POST` and `HEAD` alongside `GET`.

The major limitation: **one request per TCP connection**. Loading a page with 20 images meant 20 separate TCP connections, each with handshake overhead.

### HTTP/1.1 (1997–2022)

HTTP/1.1 was a major leap that introduced:

- **Persistent connections** — reuse TCP connections across multiple requests.
- **Host header** — required in every request, enabling virtual hosting.
- **Chunked transfer encoding** — stream responses of unknown size.
- **Content negotiation** — `Accept`, `Accept-Language`, `Accept-Encoding`.
- **Caching** — `Cache-Control`, `ETag`, conditional requests.
- **Range requests** — partial content delivery for resumable downloads.
- **Pipelining** — send multiple requests without waiting (though rarely used in practice).

The specification was revised multiple times:
- **RFC 2068** (1997) — initial specification.
- **RFC 2616** (1999) — consolidated revision, the reference for 15+ years.
- **RFC 7230–7235** (2014) — split into six focused documents for clarity.
- **RFC 9110–9112** (2022) — current standard, separating semantics from wire format.

## HTTP Today

### HTTP/1.1

Still widely deployed and **the dominant protocol** for:
- Server-to-server communication behind load balancers.
- Reverse proxies and internal APIs.
- Environments where simplicity and debuggability matter.
- Legacy systems and embedded devices.

Its text-based format makes it uniquely accessible for debugging — you can literally read the bytes on the wire.

### HTTP/2 (2015, RFC 9113)

HTTP/2 addressed HTTP/1.1's performance limitations:

- **Binary framing** — messages are encoded in binary frames instead of text. More compact and less error-prone to parse.
- **Multiplexing** — multiple concurrent request/response exchanges on a single connection, eliminating head-of-line blocking at the HTTP layer.
- **Header compression (HPACK)** — compresses headers using a static table and dynamic indexing. Headers like `Host`, `Accept`, and `User-Agent` that repeat on every request are sent efficiently.
- **Server push** — the server can proactively send resources it knows the client will need. (Largely deprecated — Chrome removed support in 2022.)
- **Stream prioritization** — clients can indicate which resources are more important.

HTTP/2 keeps the same semantics (methods, status codes, headers) as HTTP/1.1 — it only changes how messages are framed on the wire. Most HTTP/2 deployments use TLS (the `h2` protocol identifier negotiated via ALPN).

### HTTP/3 (2022, RFC 9114)

HTTP/3 replaces TCP with **QUIC**, a UDP-based transport:

- **No TCP head-of-line blocking** — packet loss on one stream doesn't block others. In HTTP/2 over TCP, a single lost packet stalls all streams.
- **0-RTT connection setup** — QUIC combines the transport and TLS handshake into a single round-trip. Resumed connections can send data immediately (0-RTT).
- **Connection migration** — a QUIC connection survives network changes (e.g., switching from Wi-Fi to cellular) because it's identified by a connection ID, not a source IP+port tuple.
- **Built-in encryption** — TLS 1.3 is mandatory and integrated into the transport layer.
- **Header compression (QPACK)** — similar to HPACK but designed for QUIC's out-of-order delivery.

## The Future

Active work in the IETF HTTP Working Group includes:

- **WebTransport** — bidirectional, multiplexed transport for web applications, built on HTTP/3. Enables use cases like game networking and live media that need both reliable and unreliable delivery.
- **HTTP Datagrams** (RFC 9297) — unreliable datagram delivery over HTTP connections. Enables latency-sensitive applications that can tolerate packet loss.
- **MASQUE proxying** — using HTTP CONNECT-UDP and CONNECT-IP for tunneling arbitrary IP and UDP traffic through HTTP proxies. Enables VPN-like functionality over HTTP infrastructure.
- **Resumable uploads** — standardizing the ability to pause and resume large file uploads (draft-ietf-httpbis-resumable-upload).
- Ongoing refinement of HTTP semantics, caching specifications, and security best practices.

## Alternatives to HTTP

HTTP is not the only application-layer protocol. Depending on the use case, other protocols may be a better fit:

| Protocol | Transport | Use Case |
|----------|-----------|----------|
| **gRPC** | HTTP/2 | High-performance RPC with Protocol Buffers. Strongly typed contracts, streaming, deadlines. Common for microservice communication. |
| **WebSocket** | TCP (HTTP Upgrade) | Full-duplex, persistent connection. Real-time applications like chat, live dashboards, collaborative editing. |
| **MQTT** | TCP | Lightweight pub/sub messaging for IoT and constrained devices. Tiny packet overhead, QoS levels, retained messages. |
| **CoAP** | UDP | Constrained Application Protocol — REST-like semantics for low-power, lossy networks. Uses UDP with optional reliability. |
| **AMQP** | TCP | Advanced Message Queuing Protocol — reliable message brokering with routing, queuing, and transactions. (RabbitMQ, Azure Service Bus.) |
| **FTP** | TCP | File transfer protocol. Still used for legacy integrations, bulk file exchange, and some hosting workflows. |
| **SMTP** | TCP | Email delivery. Purpose-built for store-and-forward message delivery across mail servers. |

## Learn More

### Videos

- [HTTP Crash Course & Explore](https://www.youtube.com/watch?v=iYM2zFP3Zn0) — Traversy Media
- [How HTTP Requests Work](https://www.youtube.com/watch?v=4_-KdOo4rGo) — LiveOverflow
- [HTTP/1 to HTTP/2 to HTTP/3](https://www.youtube.com/watch?v=a-sBfyiXysI) — Hussein Nasser

### Documentation

- [MDN: An overview of HTTP](https://developer.mozilla.org/en-US/docs/Web/HTTP/Overview) — beginner-friendly reference.
- [RFC 9110 — HTTP Semantics](https://www.rfc-editor.org/rfc/rfc9110) — the current specification for HTTP semantics.
- [RFC 9112 — HTTP/1.1](https://www.rfc-editor.org/rfc/rfc9112) — the current specification for HTTP/1.1 message syntax.
- [RFC 9113 — HTTP/2](https://www.rfc-editor.org/rfc/rfc9113) — the HTTP/2 specification.
- [RFC 9114 — HTTP/3](https://www.rfc-editor.org/rfc/rfc9114) — the HTTP/3 specification.
- [IETF HTTP Working Group](https://httpwg.org/) — active drafts, meeting notes, and mailing list.
- [High Performance Browser Networking](https://hpbn.co/) — Ilya Grigorik's free online book covering HTTP, TLS, and networking performance.
