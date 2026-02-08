---
title: "UNKNOWN-TE-501"
description: "UNKNOWN-TE-501 test documentation"
weight: 8
---

| | |
|---|---|
| **Test ID** | `COMP-UNKNOWN-TE-501` |
| **Category** | Compliance |
| **RFC** | [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1) |
| **Requirement** | SHOULD respond with 501 |
| **Expected** | `400`/`501` or close |

## What it sends

`Transfer-Encoding: gzip` without any Content-Length — an unknown transfer coding as the only framing.

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Transfer-Encoding: gzip\r\n
\r\n
```


## What the RFC says

> "A server that receives a request message with a transfer coding it does not understand SHOULD respond with 501 (Not Implemented)." -- RFC 9112 §6.1

Additionally, the chunked coding is the only transfer coding that is universally required:

> "A recipient MUST be able to parse the chunked transfer coding because it plays a crucial role in framing messages when the content size is not known in advance." -- RFC 9112 §6.1

## Why it matters

When a server doesn't understand the transfer coding and there's no Content-Length fallback, it cannot determine the message body boundaries. Rejecting or responding with 501 is correct.

## Deep Analysis

### Relevant ABNF

```
Transfer-Encoding = #transfer-coding
transfer-coding   = token *( OWS ";" OWS transfer-parameter )
```

The `Transfer-Encoding` header carries a list of transfer codings. The only transfer coding that all HTTP/1.1 implementations are required to support is `chunked`. The value `gzip` is a content coding, not a transfer coding -- when it appears alone in `Transfer-Encoding` without `chunked` as the final coding, the server likely cannot parse or frame the message body.

### RFC Evidence

The core requirement for handling unrecognized transfer codings is stated directly:

> "A server that receives a request message with a transfer coding it does not understand SHOULD respond with 501 (Not Implemented)." -- RFC 9112 Section 6.1

The universal requirement to support chunked is also relevant:

> "A recipient MUST be able to parse the chunked transfer coding because it plays a crucial role in framing messages when the content size is not known in advance." -- RFC 9112 Section 6.1

The specification also requires that non-chunked transfer codings must be followed by chunked for proper framing:

> "If any transfer coding other than chunked is applied to a request's content, the sender MUST apply chunked as the final transfer coding to ensure that the message is properly framed." -- RFC 9112 Section 6.1

### Chain of Reasoning

1. The request sends `Transfer-Encoding: gzip` as the sole transfer coding, with no `Content-Length` header. This means the only framing information available is the `Transfer-Encoding` header.
2. If the server does not understand `gzip` as a transfer coding (and most servers only recognize `chunked`), it has no way to determine the message body boundaries.
3. The RFC recommends `501 (Not Implemented)` for this scenario. A `400` is also a reasonable rejection -- the request lacks proper framing.
4. Without either a recognized transfer coding or a `Content-Length`, the server cannot safely read the body. Any attempt to guess body boundaries could lead to desynchronization on a persistent connection.
5. Connection close is also an acceptable response because it eliminates the desynchronization risk entirely.

### Scoring Justification

This test is **scored**. The RFC provides a clear SHOULD-level recommendation for `501` when the transfer coding is not understood, and the absence of any alternative framing (no `Content-Length`) makes the request unprocessable. `400`/`501` or close = **Pass**, `2xx` (processing a request with unknown framing) = **Fail**.

## Sources

- [RFC 9112 Section 6.1](https://www.rfc-editor.org/rfc/rfc9112#section-6.1)
