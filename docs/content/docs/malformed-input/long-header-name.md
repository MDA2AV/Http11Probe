---
title: "LONG-HEADER-NAME"
description: "LONG-HEADER-NAME test documentation"
weight: 3
---

| | |
|---|---|
| **Test ID** | `MAL-LONG-HEADER-NAME` |
| **Category** | Malformed Input |
| **Expected** | `400`, `431`, or close |

## What it sends

A request with a ~100 KB header field name.

```http
GET / HTTP/1.1\r\n
Host: localhost:8080\r\n
AAAA...{100,000 × 'A'}...: val\r\n
\r\n
```

The header name is 100,000 bytes of `A` characters.


## What the RFC says

> "The 431 status code indicates that the server is unwilling to process the request because its header fields are too large. The request MAY be resubmitted after reducing the size of the request header fields." — RFC 6585 Section 5

> "It can be used both when the set of request header fields in total is too large, and when a single header field is at fault. In the latter case, the response representation SHOULD specify which header field was too large." — RFC 6585 Section 5

A 100KB header name is a single field at fault. The server may respond with 431, 400, or close the connection.

## Deep Analysis

### ABNF context

The header field grammar places no upper bound on field-name length:

```
field-line  = field-name ":" OWS field-value OWS
field-name  = token
token       = 1*tchar
tchar       = "!" / "#" / "$" / "%" / "&" / "'" / "*"
            / "+" / "-" / "." / "^" / "_" / "`" / "|"
            / "~" / DIGIT / ALPHA
```

A field-name of 100,000 `A` characters is syntactically valid: each `A` is `ALPHA`, which is a `tchar`, and `token = 1*tchar` has no maximum length. The ABNF is satisfied. However, the RFC provides explicit mechanisms for servers to reject oversized fields.

### RFC evidence

> "The 431 status code indicates that the server is unwilling to process the request because its header fields are too large. The request MAY be resubmitted after reducing the size of the request header fields." -- RFC 6585 Section 5

> "It can be used both when the set of request header fields in total is too large, and when a single header field is at fault. In the latter case, the response representation SHOULD specify which header field was too large." -- RFC 6585 Section 5

> "Responses with the 431 status code MUST NOT be stored by a cache." -- RFC 6585 Section 5

> "It is RECOMMENDED that all HTTP senders and recipients support, at a minimum, request-line lengths of 8000 octets." -- RFC 9112 Section 3

> "The 400 (Bad Request) status code indicates that the server cannot or will not process the request due to something that is perceived to be a client error (e.g., malformed request syntax, invalid request message framing, or deceptive request routing)." -- RFC 9110 Section 15.5.1

While RFC 9112 Section 3's 8000-octet recommendation applies to the request-line specifically, it establishes a precedent for reasonable size limits. A single 100KB header field-name dwarfs even generous limits. RFC 6585 Section 5 provides the specific 431 status code for exactly this scenario.

### Chain of reasoning

1. The server begins reading the header section after the request-line.
2. It encounters a field-line starting with 100,000 `A` characters followed by `: val`.
3. The server attempts to read the field-name, which requires buffering 100,000 bytes before reaching the colon.
4. A well-implemented server enforces a maximum field-line or field-name length (typically 8KB-64KB).
5. When the limit is exceeded, the server stops reading and responds with 431 (single header too large) or 400 (general client error).
6. Per RFC 6585 Section 5, if the server uses 431, it SHOULD indicate which header was at fault.
7. Closing the connection without a response is also acceptable as a protective measure.

### Security implications

- **Memory exhaustion (DoS)**: If the server buffers the entire field-name before checking its validity, a single request consumes 100KB of memory just for one header name. An attacker sending many such requests can rapidly exhaust server memory.
- **Hash-table performance degradation**: Servers typically store headers in a hash table keyed by field-name. A 100KB key causes excessive memory allocation for the hash entry, and if multiple such headers are sent, the hash table's memory usage grows proportionally.
- **Buffer overflow**: In languages without automatic bounds checking (C, C++), reading an unbounded field-name into a fixed-size buffer causes a classic stack or heap buffer overflow, potentially enabling code execution.
- **Log injection and storage exhaustion**: If the server logs unrecognized headers, a 100KB header name fills log files rapidly, potentially exhausting disk space and disrupting log rotation and monitoring systems.
- **Proxy forwarding amplification**: Some proxies buffer and forward all headers. A 100KB header name is forwarded to the backend, amplifying the attack through the infrastructure chain.

## Sources

- [RFC 6585 Section 5](https://www.rfc-editor.org/rfc/rfc6585#section-5) — 431 Request Header Fields Too Large
