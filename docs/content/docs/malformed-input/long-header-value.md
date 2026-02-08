---
title: "LONG-HEADER-VALUE"
description: "LONG-HEADER-VALUE test documentation"
weight: 4
---

| | |
|---|---|
| **Test ID** | `MAL-LONG-HEADER-VALUE` |
| **Category** | Malformed Input |
| **Expected** | `400`, `431`, or close |

## What it sends

A request with a ~100 KB header field value.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
X-Big: BBBB...{100,000 × 'B'}...\r\n
\r\n
```

The `X-Big` header value is 100,000 bytes of `B` characters.


## What the RFC says

> "The 431 status code indicates that the server is unwilling to process the request because its header fields are too large. The request MAY be resubmitted after reducing the size of the request header fields." — RFC 6585 Section 5

> "It can be used both when the set of request header fields in total is too large, and when a single header field is at fault. In the latter case, the response representation SHOULD specify which header field was too large." — RFC 6585 Section 5

A 100KB header value far exceeds any reasonable limit. The server may respond with 431, 400 (general client error), or close the connection.

## Deep Analysis

### ABNF context

The field-value grammar places no upper bound on value length:

```
field-line    = field-name ":" OWS field-value OWS
field-value   = *field-content
field-content = field-vchar
                [ 1*( SP / HTAB / field-vchar ) field-vchar ]
field-vchar   = VCHAR / obs-text
VCHAR         = %x21-7E
```

A field-value of 100,000 `B` characters is syntactically valid: each `B` is `VCHAR` (`0x42`), therefore `field-vchar`, and `field-value = *field-content` permits any number of `field-content` repetitions. The ABNF has no maximum length. However, the RFC provides explicit mechanisms for servers to reject oversized fields.

### RFC evidence

> "The 431 status code indicates that the server is unwilling to process the request because its header fields are too large. The request MAY be resubmitted after reducing the size of the request header fields." -- RFC 6585 Section 5

> "It can be used both when the set of request header fields in total is too large, and when a single header field is at fault. In the latter case, the response representation SHOULD specify which header field was too large." -- RFC 6585 Section 5

> "Responses with the 431 status code MUST NOT be stored by a cache." -- RFC 6585 Section 5

> "The 400 (Bad Request) status code indicates that the server cannot or will not process the request due to something that is perceived to be a client error (e.g., malformed request syntax, invalid request message framing, or deceptive request routing)." -- RFC 9110 Section 15.5.1

> "When a server listening only for HTTP request messages, or processing what appears from the start-line to be an HTTP request message, receives a sequence of octets that does not match the HTTP-message grammar aside from the robustness exceptions listed above, the server SHOULD respond with a 400 (Bad Request) response and close the connection." -- RFC 9112 Section 2.2

RFC 6585 Section 5 was specifically designed for this scenario. The 431 status code applies both to the aggregate size of all headers and to a single header field at fault. A 100KB value for a single header clearly falls into the "single header field is at fault" category.

### Chain of reasoning

1. The server reads the header section and encounters `X-Big: BBBB...` (100,000 bytes of `B`).
2. It begins buffering the field-value, reading `VCHAR` characters until it reaches `\r\n`.
3. A well-implemented server enforces a maximum field-value or total-field-line length.
4. After exceeding the limit (e.g., 8KB, 16KB, or 64KB depending on implementation), the server stops reading.
5. It responds with 431 (indicating the `X-Big` header is too large) or 400 (general rejection) and closes the connection.
6. If the server has no limit and buffers the entire 100KB value, it has consumed significant memory for a single header of a single request -- a resource that an attacker can exploit at scale.

### Security implications

- **Memory exhaustion (DoS)**: Each request with a 100KB header value consumes 100KB of memory. At 10,000 concurrent connections, that is 1GB just for one header per request. Servers without limits are vulnerable to memory-based denial of service.
- **Hash-table storage amplification**: The header value is typically stored in the server's header hash table. A 100KB value plus its key and metadata consumes far more memory than a typical header, and the server may allocate this memory before processing or rejecting the request.
- **Buffer overflow**: Fixed-size buffers in C/C++ servers that do not check lengths before copying header values are vulnerable to heap or stack overflows, potentially leading to remote code execution.
- **Application-layer impact**: Even if the server's HTTP parser handles the oversized header correctly, the application layer may process it unsafely. For example, if `X-Big` is logged, displayed, or stored in a database, the 100KB value can cause log bloat, UI rendering issues, or database column overflow.
- **Proxy amplification**: Proxies that forward all headers verbatim pass the 100KB value to the backend, multiplying the memory impact across the infrastructure chain. If the proxy has different limits than the backend, the disagreement creates a potential for differential behavior exploitation.

## Sources

- [RFC 6585 Section 5](https://www.rfc-editor.org/rfc/rfc6585#section-5) — 431 Request Header Fields Too Large
