---
title: "CL-OVERFLOW"
description: "CL-OVERFLOW test documentation"
weight: 11
---

| | |
|---|---|
| **Test ID** | `MAL-CL-OVERFLOW` |
| **Category** | Malformed Input |
| **Expected** | `400` or close |

## What it sends

A `Content-Length` value exceeding the 64-bit integer range (e.g., `99999999999999999999`).

```http
POST / HTTP/1.1\r\n
Host: localhost:8080\r\n
Content-Length: 99999999999999999999\r\n
\r\n
```

The Content-Length value exceeds 64-bit integer range.


## What the RFC says

> `Content-Length = 1*DIGIT` — RFC 9110 Section 8.6

While the grammar allows any number of digits, the value must represent a valid decimal number for the message body length. A value like `99999999999999999999` exceeds the 64-bit unsigned integer range (max 18,446,744,073,709,551,615), making it impossible to interpret as a body length.

> "The 400 (Bad Request) status code indicates that the server cannot or will not process the request due to something that is perceived to be a client error." — RFC 9110 Section 15.5.1

## Why it matters

If a parser uses a fixed-width integer without overflow checking, the parsed value wraps around. This can lead to reading a different amount of body data than intended -- a smuggling vector.

## Deep Analysis

### ABNF context

The Content-Length grammar is simple but has no upper bound:

```
Content-Length = 1*DIGIT
DIGIT          = %x30-39   ; 0-9
```

The value `99999999999999999999` (20 decimal digits) matches `1*DIGIT` syntactically -- every character is a valid digit. However, the numeric value (9.999... x 10^19) exceeds the maximum 64-bit unsigned integer (18,446,744,073,709,551,615 = ~1.844 x 10^19). While the ABNF is satisfied, the value cannot be represented in any standard integer type and cannot correspond to an actual body length.

### RFC evidence

> "Content-Length = 1*DIGIT" -- RFC 9110 Section 8.6

> "A sender MUST NOT send a Content-Length header field in any message that contains a Transfer-Encoding header field." -- RFC 9112 Section 6.1

> "When a server listening only for HTTP request messages, or processing what appears from the start-line to be an HTTP request message, receives a sequence of octets that does not match the HTTP-message grammar aside from the robustness exceptions listed above, the server SHOULD respond with a 400 (Bad Request) response and close the connection." -- RFC 9112 Section 2.2

> "The 400 (Bad Request) status code indicates that the server cannot or will not process the request due to something that is perceived to be a client error (e.g., malformed request syntax, invalid request message framing, or deceptive request routing)." -- RFC 9110 Section 15.5.1

While `1*DIGIT` has no syntactic upper bound, the value must represent a meaningful body length. A value that overflows the server's integer type constitutes "invalid request message framing" -- the server cannot determine where the body ends.

### Chain of reasoning

1. The server receives a POST with `Content-Length: 99999999999999999999`.
2. It parses the field-value and attempts to convert `99999999999999999999` to an integer.
3. The value exceeds the 64-bit unsigned integer maximum (2^64 - 1 = 18,446,744,073,709,551,615).
4. If the server detects the overflow, it rejects with 400 -- the correct behavior.
5. If the server does NOT detect the overflow and uses a 64-bit integer, the parsed value wraps around to `99999999999999999999 mod 2^64 = 3,553,255,926,290,448,383` -- a completely different body length.
6. The server would then attempt to read ~3.5 exabytes of body data (still impossibly large) or, if further truncated to 32 bits, a much smaller value that could actually be satisfied by subsequent data on the connection.
7. This framing corruption enables smuggling: the server reads a different number of bytes than the client intended, and leftover bytes become the "next request."

### Security implications

- **Request smuggling via integer wrap**: If the overflowed Content-Length wraps to a small value (especially on 32-bit systems where `99999999999999999999 mod 2^32 = 3,567,587,327`), an attacker can craft a payload where the first N bytes satisfy the wrapped Content-Length, and the remaining bytes are interpreted as a new smuggled request.
- **Memory exhaustion**: If the server trusts the parsed (pre-overflow) value and attempts to allocate a buffer of that size, it will try to allocate more memory than physically exists, causing an OOM crash.
- **Differential parsing**: A 64-bit proxy and a 32-bit backend will compute different wrapped values from the same Content-Length, leading to framing disagreement and smuggling opportunities.
- **Timeout-based DoS**: A server that accepts the value and begins waiting for ~10^19 bytes of body data will hold the connection open indefinitely, consuming a socket and associated resources.

## Sources

- [RFC 9110 Section 8.6](https://www.rfc-editor.org/rfc/rfc9110#section-8.6) — Content-Length grammar
- [RFC 9110 Section 15.5.1](https://www.rfc-editor.org/rfc/rfc9110#section-15.5.1) — 400 Bad Request
