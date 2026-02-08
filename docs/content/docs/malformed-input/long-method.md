---
title: "LONG-METHOD"
description: "LONG-METHOD test documentation"
weight: 5
---

| | |
|---|---|
| **Test ID** | `MAL-LONG-METHOD` |
| **Category** | Malformed Input |
| **Expected** | `400` or close |

## What it sends

A request with a ~100 KB method name.

```http
AAAA...{100,000 × 'A'}... / HTTP/1.1\r\n
Host: localhost:8080\r\n
\r\n
```

The HTTP method is 100,000 bytes of `A` characters.


## What the RFC says

> "A server that receives a method longer than any that it implements SHOULD respond with a 501 (Not Implemented) status code." — RFC 9112 Section 3

> "It is RECOMMENDED that all HTTP senders and recipients support, at a minimum, request-line lengths of 8000 octets." — RFC 9112 Section 3

The method is part of the request-line. A 100KB method vastly exceeds the recommended 8000-octet minimum. The server may respond with 400, 501, or close the connection.

> "The 400 (Bad Request) status code indicates that the server cannot or will not process the request due to something that is perceived to be a client error." — RFC 9110 Section 15.5.1

## Why it matters

Methods are tokens with no defined maximum length, but 100 KB exceeds any reasonable limit. A server that buffers this risks memory exhaustion.

## Deep Analysis

### ABNF context

The method is a token with no upper bound:

```
request-line = method SP request-target SP HTTP-version
method       = token
token        = 1*tchar
tchar        = "!" / "#" / "$" / "%" / "&" / "'" / "*"
             / "+" / "-" / "." / "^" / "_" / "`" / "|"
             / "~" / DIGIT / ALPHA
```

A method of 100,000 `A` characters matches `token = 1*tchar` perfectly -- each `A` is `ALPHA`, which is a `tchar`. The ABNF places no upper bound. However, the entire request-line (method + SP + request-target + SP + HTTP-version) is 100,000 + 1 + 1 + 1 + 8 = 100,011 bytes, far exceeding the RFC's recommended minimum of 8,000 octets.

### RFC evidence

> "A server that receives a method longer than any that it implements SHOULD respond with a 501 (Not Implemented) status code." -- RFC 9112 Section 3

> "It is RECOMMENDED that all HTTP senders and recipients support, at a minimum, request-line lengths of 8000 octets." -- RFC 9112 Section 3

> "A server that receives a request-target longer than any URI it wishes to parse MUST respond with a 414 (URI Too Long) status code." -- RFC 9112 Section 3

> "When a server listening only for HTTP request messages, or processing what appears from the start-line to be an HTTP request message, receives a sequence of octets that does not match the HTTP-message grammar aside from the robustness exceptions listed above, the server SHOULD respond with a 400 (Bad Request) response and close the connection." -- RFC 9112 Section 2.2

The RFC provides multiple rejection mechanisms: 501 for unrecognized methods, 414 for oversized request-targets (and by analogy, oversized request-lines), and 400 for general grammar violations. The RECOMMENDED 8,000-octet minimum means a server that supports only 8,000 octets is conformant -- and a 100,011-byte request-line exceeds that by a factor of 12.5.

### Chain of reasoning

1. The server begins reading the request-line, expecting `method SP request-target SP HTTP-version`.
2. It reads `tchar` bytes for the method: `AAAA...` (100,000 bytes).
3. A well-implemented server enforces a maximum request-line length. After exceeding its limit (e.g., 8KB), it stops reading.
4. Multiple appropriate responses exist:
   - **400**: The request-line exceeds the server's length limit, making it unparseable.
   - **501**: The method is longer than any implemented method (GET, POST, etc. are all under 10 bytes), triggering the SHOULD-501 from RFC 9112 Section 3.
   - **Connection close**: The server may simply close the connection without responding, especially if it cannot even parse enough of the request to formulate a response.
5. The 100KB method is not merely an unrecognized method name -- it is a resource exhaustion attack disguised as a method token.

### Security implications

- **Memory exhaustion (DoS)**: If the server buffers the entire method before checking if it is recognized, each request consumes 100KB just for the method string. An attacker can send thousands of such requests to exhaust server memory.
- **Buffer overflow**: In C/C++ servers, a fixed-size buffer for the method (e.g., `char method[256]`) will overflow when reading 100,000 bytes, potentially enabling remote code execution.
- **Request-line parsing stall**: The server reads `tchar` bytes looking for the first `SP` delimiter. With 100,000 bytes before the space, the parser spends significant CPU time in the read loop, reducing throughput for legitimate requests.
- **Log injection and storage exhaustion**: If the server logs the method name (as many access logs do), a 100KB method fills log storage rapidly. At 1,000 requests per second, that is 100MB/s of log data from the method alone.
- **WAF and IDS evasion**: Security devices that inspect the method field may have their own buffer limits. A 100KB method may cause the security device to truncate or skip inspection, while the backend server processes it differently.

## Sources

- [RFC 9112 Section 3](https://www.rfc-editor.org/rfc/rfc9112#section-3) — request-line and method length
- [RFC 9110 Section 15.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-15.5.1) — 400 Bad Request
